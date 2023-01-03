using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Nethermind.Evm;
using Nethermind.Specs;

namespace Nethermind.Evm.EOF;

public interface IEofVersionHandler
{
    Result ValidateCode(ReadOnlySpan<byte> code);
    Result TryParseEofHeader(ReadOnlySpan<byte> code, out EofHeader? header);
}

public class EvmObjectFormat
{
    private static byte[] EOF_MAGIC = { 0xEF, 0x00 };

    private readonly Dictionary<byte, IEofVersionHandler> _eofVersionHandlers = new();

    public EvmObjectFormat(IReleaseSpec releaseSpec)
    {
        _eofVersionHandlers.Add(0x01, new Eof1(releaseSpec));
    }

    public bool IsEof(ReadOnlySpan<byte> container) => container.StartsWith(EOF_MAGIC);

    public Result IsValidEof(ReadOnlySpan<byte> container)
    {
        if (!IsEof(container) || container.Length < 7)
            return Failure<string>.From("Invalid Eof Prefix");
        return _eofVersionHandlers.ContainsKey(container[2])
            ? _eofVersionHandlers[container[2]].ValidateCode(container) // will handle rest of validations
            : Failure<string>.From("Invalid Eof version {container[2]}");
    }

    public class Eof1 : IEofVersionHandler
    {
        private IReleaseSpec _releaseSpec;
        private const byte VERSION = 0x01;

        private const byte KIND_TYPE = 0x01;
        private const byte KIND_CODE = 0x02;
        private const byte KIND_DATA = 0x03;
        private const byte TERMINATOR = 0x00;

        private const byte VERSION_SIZE = 1;
        private const byte SECTION_SIZE = 3;
        private const byte TERMINATOR_SIZE = 1;

        private const byte MINIMUM_TYPESECTION_SIZE = 4;
        private const byte MINIMUM_CODESECTION_SIZE = 1;

        private const ushort MINIMUM_CODESECTIONS_COUNT = 1;
        private const ushort MAXIMUM_CODESECTIONS_COUNT = 1024;
        private const ushort MAXIMUM_DATA_STACKHEIGHT = 1023;

        private const byte IMMEDIATE_16BIT_BYTE_COUNT = 2;
        public static int MINIMUM_HEADER_SIZE => CalculateHeaderSize(1);
        public static int CalculateHeaderSize(int numberOfSections) => EOF_MAGIC.Length + VERSION_SIZE
            + SECTION_SIZE // type
            + GetArraySectionSize(numberOfSections) // code
            + SECTION_SIZE // data
            + TERMINATOR_SIZE;

        public static int GetArraySectionSize(int numberOfSections) => 3 + numberOfSections * 2;
        private bool CheckBounds(int index, int length, ref EofHeader? header)
        {
            if (index >= length)
            {
                header = null;
                return false;
            }
            return true;
        }

        public Eof1(IReleaseSpec spec)
        {
            _releaseSpec = spec;
        }

        public Result ValidateCode(ReadOnlySpan<byte> container)
        {
            EofHeader? header = null;
            var result = 
                TryParseEofHeader(container, out header) is Failure<string> headerParsingFailure 
                ?   headerParsingFailure
                :   ValidateBody(container, ref header) is Failure<string> bodyValidationFailure
                    ? bodyValidationFailure
                    : ValidateInstructions(container, ref header);

            if(result is Failure<string> failure)
            {
                return failure;
            } else {
                return Success<EofHeader?>.From(header);
            }
        }

        public Result TryParseEofHeader(ReadOnlySpan<byte> container, [NotNullWhen(true)] out EofHeader? header)
        {
            header = null;
            if (!container.StartsWith(EOF_MAGIC))
            {
                return Failure<String>.From($"EIP-3540 : Code doesn't start with Magic byte sequence expected {0xef00} ");
            }
            if (container[EOF_MAGIC.Length] != VERSION)
            {
                return Failure<String>.From($"EIP-3540 : Code is not Eof version {VERSION}");
            }

            if (container.Length < MINIMUM_HEADER_SIZE
                + MINIMUM_TYPESECTION_SIZE // minimum type section body size
                + MINIMUM_CODESECTION_SIZE) // minimum code section body size
            {
                return Failure<String>.From($"EIP-3540 : Eof{VERSION}, Code is too small to be valid code");
            }


            ushort numberOfCodeSections = container[7..9].ReadEthUInt16();
            if (numberOfCodeSections < MINIMUM_CODESECTIONS_COUNT)
            {
                return Failure<String>.From($"EIP-3540 : At least one code section must be present");
            }

            if (numberOfCodeSections > MAXIMUM_CODESECTIONS_COUNT)
            {
                return Failure<String>.From($"EIP-3540 : code sections count must not exceed 1024");
            }

            int headerSize = CalculateHeaderSize(numberOfCodeSections);
            int pos = 3;

            if (container[pos] != KIND_TYPE)
            {
                return Failure<String>.From($"EIP-3540 : Eof{VERSION}, Code header is not well formatted");
            }

            pos++;
            if (!CheckBounds(pos + IMMEDIATE_16BIT_BYTE_COUNT, container.Length, ref header))
            {
                return Failure<String>.From($"EIP-3540 : Eof{VERSION}, Code header is not well formatted");
            }

            SectionHeader typeSection = new()
            {
                Start = headerSize,
                Size = container[pos..(pos + IMMEDIATE_16BIT_BYTE_COUNT)].ReadEthUInt16()
            };

            if (typeSection.Size < MINIMUM_TYPESECTION_SIZE)
            {
                return Failure<String>.From($"EIP-3540 : TypeSection Size must be at least 4, but found {typeSection.Size}");
            }

            pos += IMMEDIATE_16BIT_BYTE_COUNT;

            if (container[pos] != KIND_CODE)
            {
                return Failure<String>.From($"EIP-3540 : Eof{VERSION}, Code header is not well formatted");
            }

            pos += 3; // kind_code(1) + num_code_sections(2)
            if (!CheckBounds(pos, container.Length, ref header))
            {
                return Failure<String>.From($"EIP-3540 : Eof{VERSION}, Code header is not well formatted");
            }

            List<SectionHeader> codeSections = new();
            int lastEndOffset = typeSection.EndOffset;
            for (ushort i = 0; i < numberOfCodeSections; i++)
            {
                if (!CheckBounds(pos + IMMEDIATE_16BIT_BYTE_COUNT, container.Length, ref header))
                {
                    header = null;
                    return Failure<String>.From($"EIP-3540 : Eof{VERSION}, Code header is not well formatted");
                }

                SectionHeader codeSection = new()
                {
                    Start = lastEndOffset,
                    Size = container[pos..(pos + IMMEDIATE_16BIT_BYTE_COUNT)].ReadEthUInt16()
                };

                if (codeSection.Size == 0)
                {
                    header = null;
                    return Failure<String>.From($"EIP-3540 : Empty Code Section are not allowed, CodeSectionSize must be > 0 but found {codeSection.Size}");
                }

                codeSections.Add(codeSection);
                lastEndOffset = codeSection.EndOffset;
                pos += IMMEDIATE_16BIT_BYTE_COUNT;

            }


            if (container[pos] != KIND_DATA)
            {
                return Failure<String>.From($"EIP-3540 : Eof{VERSION}, Code header is not well formatted");
            }
            pos++;
            if (!CheckBounds(pos + IMMEDIATE_16BIT_BYTE_COUNT, container.Length, ref header))
            {
                return Failure<String>.From($"EIP-3540 : Eof{VERSION}, Code header is not well formatted");
            }

            SectionHeader dataSection = new()
            {
                Start = lastEndOffset,
                Size = container[(pos)..(pos + IMMEDIATE_16BIT_BYTE_COUNT)].ReadEthUInt16()
            };
            pos += IMMEDIATE_16BIT_BYTE_COUNT;


            if (container[pos] != TERMINATOR)
            {
                return Failure<String>.From($"EIP-3540 : Eof{VERSION}, Code header is not well formatted");
            }

            header = new EofHeader
            {
                Version = VERSION,
                TypeSection = typeSection,
                CodeSections = codeSections.ToArray(),
                DataSection = dataSection
            };
            return Success<bool>.From(true);
        }

        Result ValidateBody(ReadOnlySpan<byte> container, ref EofHeader? header)
        {
            if(header == null)
            {
                return Failure<String>.From($"EIP-3540 : Code header is not well formatted");
            }

            SectionHeader[]? codeSections = header.Value.CodeSections;
            (int typeSectionStart, ushort typeSectionSize) = header.Value.TypeSection;
            var typesection = container.Slice(typeSectionStart, typeSectionSize);
            if (codeSections.Length == 0 || codeSections.Any(section => section.Size == 0))
            {
                header = null;
                return Failure<String>.From($"EIP-3540 : CodeSection size must follow a CodeSection, CodeSection length was {codeSections.Length}");
            }

            if (codeSections.Length != (typeSectionSize / MINIMUM_TYPESECTION_SIZE))
            {
                header = null;
                return Failure<String>.From($"EIP-3540: Code Sections count must match TypeSection count, CodeSection count was {codeSections.Length}, expected {typeSectionSize / MINIMUM_TYPESECTION_SIZE}");
            }

            if (container[typeSectionStart] != 0 || container[typeSectionStart + 1] != 0)
            {
                header = null;
                return Failure<String>.From($"EIP-3540: first 2 bytes of type section must be 0s");
            }

            if (codeSections.Length > MAXIMUM_CODESECTIONS_COUNT)
            {
                header = null;
                return Failure<String>.From($"EIP-4750 : Code section count limit exceeded only {MAXIMUM_CODESECTIONS_COUNT} allowed but found {codeSections.Length}");
            }

            int startOffset = CalculateHeaderSize(header.Value.CodeSections.Length);
            int calculatedCodeLength = header.Value.TypeSection.Size
                + header.Value.CodeSections.Sum(c => c.Size)
                + header.Value.DataSection.Size;
            
            ReadOnlySpan<byte> contractBody = container[startOffset..];

            if (contractBody.Length != calculatedCodeLength)
            {
                header = null;
                return Failure<String>.From($"EIP-3540 : CodeSection size must follow a CodeSection, CodeSection length was {codeSections.Length}");
            }

            if(ValidateTypeSection(typesection) is Failure<String> failure)
            {
                header = null;
                return failure;
            }

            return Success<bool>.From(true);
        }

        public Result ValidateTypeSection(ReadOnlySpan<byte> types)
        {
            if (types[0] != 0 || types[1] != 0)
            {
                return Failure<String>.From($"EIP-4750 : first 2 bytes of code section must be 0s but found {types[0]}{types[1]}");
            }

            if (types.Length % MINIMUM_TYPESECTION_SIZE != 0)
            {
                return Failure<String>.From($"EIP-4750: type section length must be a product of {MINIMUM_TYPESECTION_SIZE}");
            }

            for (int offset = 0; offset < types.Length; offset += MINIMUM_TYPESECTION_SIZE)
            {
                byte inputCount = types[offset + 0];
                byte outputCount = types[offset + 1];
                ushort maxStackHeight = types.Slice(offset + 2, 2).ReadEthUInt16();

                if (inputCount > 0x7F)
                {
                    return Failure<String>.From($"EIP-4750 : Too many inputs {inputCount}");
                }

                if (outputCount > 0x7F)
                {
                    return Failure<String>.From($"EIP-4750 : Too many outputs {outputCount}");
                }

                if (maxStackHeight > 0x3FF)
                {
                    return Failure<String>.From($"EIP-3540 : Stack depth too high {maxStackHeight}");
                }
            }
            return Success<bool>.From(true);
        }

        public Result ValidateInstructions(ReadOnlySpan<byte> container, ref EofHeader? header)
        {
            if (!_releaseSpec.IsEip3670Enabled)
            {
                return Success<bool>.From(true);
            }

            if (header == null)
            {
                return Failure<String>.From($"EIP-3540 : Code header is not well formatted");
            }

            Result valid = Success<bool>.From(true);
            for (int sectionId = 0; sectionId < header.Value.CodeSections.Length; sectionId++)
            {
                var (typeSectionBegin, typeSectionSize) = header.Value.TypeSection;
                var (codeSectionBegin, codeSectionSize) = header.Value.CodeSections[sectionId];

                ReadOnlySpan<byte> code = container.Slice(codeSectionBegin, codeSectionSize);
                ReadOnlySpan<byte> typesection = container.Slice(typeSectionBegin, typeSectionSize);

                valid  = ValidateSectionInstructions(sectionId, in code, in typesection, ref header) is Failure<string> error 
                    ? error 
                    : ValidateStackState(sectionId, in code, in typesection, ref header);
                if(valid is Failure<string> failure)
                {
                    return failure;
                }
            }
            return valid;
        }

        public Result ValidateSectionInstructions(int sectionId, in ReadOnlySpan<byte> code, in ReadOnlySpan<byte> typesection, ref EofHeader? header)
        {
            Instruction? opcode = null;

            HashSet<Range> immediates = new();
            HashSet<Int32> rjumpdests = new();
            for (int i = 0; i < code.Length;)
            {
                opcode = (Instruction)code[i];
                i++;
                // validate opcode
                if (!opcode.Value.IsValid(_releaseSpec, true))
                {
                    header = null;
                    return Failure<String>.From($"EIP-3670 : CodeSection contains undefined opcode {opcode}");
                }

                if (_releaseSpec.StaticRelativeJumpsEnabled)
                {
                    if (opcode is Instruction.RJUMP or Instruction.RJUMPI)
                    {
                        if (i + IMMEDIATE_16BIT_BYTE_COUNT > code.Length)
                        {
                            header = null;
                            return Failure<String>.From($"EIP-4200 : Static Relative Jump Argument underflow");
                        }

                        var offset = code.Slice(i, IMMEDIATE_16BIT_BYTE_COUNT).ReadEthInt16();
                        immediates.Add(new Range(i, i + 1));
                        var rjumpdest = offset + IMMEDIATE_16BIT_BYTE_COUNT + i;
                        rjumpdests.Add(rjumpdest);
                        if (rjumpdest < 0 || rjumpdest >= code.Length)
                        {
                            header = null;
                            return Failure<String>.From($"EIP-4200 : Static Relative Jump Destination outside of Code bounds");
                        }
                        i += 2;
                    }

                    if (opcode is Instruction.RJUMPV)
                    {
                        if (i + IMMEDIATE_16BIT_BYTE_COUNT > code.Length)
                        {
                            header = null;
                            return Failure<String>.From($"EIP-4200 : Static Relative Jumpv Argument underflow");
                        }

                        byte count = code[i];
                        if (count < 1)
                        {
                            header = null;
                            return Failure<String>.From($"EIP-4200 : Static Relative Jumpv jumptable must have at least 1 entry");
                        }

                        if (i + 1 + count * IMMEDIATE_16BIT_BYTE_COUNT > code.Length)
                        {
                            header = null;
                            return Failure<String>.From($"EIP-4200 : Static Relative Jumpv jumptable underflow");
                        }

                        var immediateValueSize = 1 + count * IMMEDIATE_16BIT_BYTE_COUNT;
                        immediates.Add(new Range(i, i + immediateValueSize - 1));
                        for (int j = 0; j < count; j++)
                        {
                            var offset = code.Slice(i + 1 + j * IMMEDIATE_16BIT_BYTE_COUNT, IMMEDIATE_16BIT_BYTE_COUNT).ReadEthInt16();
                            var rjumpdest = offset + immediateValueSize + i;
                            rjumpdests.Add(rjumpdest);
                            if (rjumpdest < 0 || rjumpdest >= code.Length)
                            {
                                header = null;
                                return Failure<String>.From($"EIP-4200 : Static Relative Jumpv Destination outside of Code bounds");
                            }
                        }
                        i += immediateValueSize;
                    }
                }

                if (_releaseSpec.FunctionSections)
                {
                    if (opcode is Instruction.CALLF)
                    {
                        if (i + IMMEDIATE_16BIT_BYTE_COUNT > code.Length)
                        {
                            header = null;
                            return Failure<String>.From($"EIP-4750 : CALLF Argument underflow");
                        }

                        ushort targetSectionId = code.Slice(i, IMMEDIATE_16BIT_BYTE_COUNT).ReadEthUInt16();
                        immediates.Add(new Range(i, i + 1));

                        if (targetSectionId >= header.Value.CodeSections.Length)
                        {
                            header = null;
                            return Failure<String>.From($"EIP-4750 : CALLF Target Section Id not found");
                        }
                        i += IMMEDIATE_16BIT_BYTE_COUNT;
                    }
                }

                if (opcode is >= Instruction.PUSH1 and <= Instruction.PUSH32)
                {
                    int len = code[i - 1] - (int)Instruction.PUSH1 + 1;
                    immediates.Add(new Range(i, i + len - 1));
                    i += len;
                }

                if (i > code.Length)
                {
                    header = null;
                    return Failure<String>.From($"EIP-3670 : PC Reached out of bounds");
                }
            }

            if (_releaseSpec.StaticRelativeJumpsEnabled)
            {

                foreach (int rjumpdest in rjumpdests)
                {
                    foreach (Range range in immediates)
                    {
                        if (range.Includes(rjumpdest))
                        {
                            header = null;
                            return Failure<String>.From($"EIP-4200 : Static Relative Jump destination {rjumpdest} is an Invalid, falls within {range}");
                        }
                    }
                }
            }
            return Success<bool>.From(true);
        }
        public Result ValidateReachableCode(int sectionId, in ReadOnlySpan<byte> code, Dictionary<int, int>.KeyCollection reachedOpcode, in EofHeader? header)
        {
            for (int i = 0; i < code.Length;)
            {
                var opcode = (Instruction)code[i];

                if (!reachedOpcode.Contains(i))
                {
                    return Failure<String>.From($"EIP-3670 : Unreachable Code at {i}");
                }

                i++;
                if (opcode is Instruction.RJUMP or Instruction.RJUMPI or Instruction.CALLF)
                {
                    i += IMMEDIATE_16BIT_BYTE_COUNT;
                }
                else if (opcode is Instruction.RJUMPV)
                {
                    byte count = code[i];

                    i += 1 + count * IMMEDIATE_16BIT_BYTE_COUNT;
                }
                else if (opcode is >= Instruction.PUSH1 and <= Instruction.PUSH32)
                {
                    int len = code[i - 1] - (int)Instruction.PUSH1 + 1;
                    i += len;
                }
            }
            return Success<bool>.From(true);
        }

        public Result ValidateStackState(int sectionId, in ReadOnlySpan<byte> code, in ReadOnlySpan<byte> typesection, ref EofHeader? header)
        {
            if (!_releaseSpec.IsEip5450Enabled)
            {
                return Success<bool>.From(true);
            }

            Dictionary<int, int> recordedStackHeight = new();
            int peakStackHeight = typesection[sectionId * 4];
            ushort suggestedMaxHeight = typesection[(sectionId * MINIMUM_TYPESECTION_SIZE + 2)..(sectionId * MINIMUM_TYPESECTION_SIZE + 2 + IMMEDIATE_16BIT_BYTE_COUNT)].ReadEthUInt16();

            Stack<(int Position, int StackHeigth)> workSet = new();
            workSet.Push((0, peakStackHeight));

            while (workSet.TryPop(out var worklet))
            {
                (int pos, int stackHeight) = worklet;
                bool stop = false;

                while (!stop)
                {
                    Instruction opcode = (Instruction)code[pos];
                    (int inputs, int outputs, int immediates) = opcode.StackRequirements();

                    if (recordedStackHeight.ContainsKey(pos))
                    {
                        if (stackHeight != recordedStackHeight[pos])
                        {
                            header = null;
                            return Failure<String>.From($"EIP-5450 : Branch joint line has invalid stack height");
                        }
                        break;
                    }
                    else
                    {
                        recordedStackHeight[pos] = stackHeight;
                    }

                    if (opcode is Instruction.CALLF)
                    {
                        var sectionIndex = code.Slice(pos + 1, IMMEDIATE_16BIT_BYTE_COUNT).ReadEthUInt16();
                        inputs = typesection[sectionIndex * MINIMUM_TYPESECTION_SIZE];
                        outputs = typesection[sectionIndex * MINIMUM_TYPESECTION_SIZE + 1];
                    }

                    if (stackHeight < inputs)
                    {
                        header = null;
                        return Failure<String>.From($"EIP-5450 : Stack Underflow required {inputs} but found {stackHeight}");
                    }

                    stackHeight += outputs - inputs;
                    peakStackHeight = Math.Max(peakStackHeight, stackHeight);

                    switch (opcode)
                    {
                        case Instruction.RJUMP:
                            {
                                var offset = code.Slice(pos + 1, IMMEDIATE_16BIT_BYTE_COUNT).ReadEthInt16();
                                var jumpDestination = pos + immediates + 1 + offset;
                                pos += jumpDestination;
                                break;
                            }
                        case Instruction.RJUMPI:
                            {
                                var offset = code.Slice(pos + 1, IMMEDIATE_16BIT_BYTE_COUNT).ReadEthInt16();
                                var jumpDestination = pos + immediates + 1 + offset;
                                workSet.Push((jumpDestination, stackHeight));
                                pos += immediates + 1;
                                break;
                            }
                        case Instruction.RJUMPV:
                            {
                                var count = code[pos + 1];
                                immediates = count * IMMEDIATE_16BIT_BYTE_COUNT + 1;
                                for (short j = 0; j < count; j++)
                                {
                                    int case_v = pos + 1 + 1 + j * IMMEDIATE_16BIT_BYTE_COUNT;
                                    int offset = code.Slice(case_v, 2).ReadEthInt16();
                                    int jumptDestination = pos + immediates + 1 + offset;
                                    workSet.Push((jumptDestination, stackHeight));
                                }
                                pos += immediates + 1;
                                break;
                            }
                        default : 
                            {
                                pos += 1 + immediates;
                                break;
                            }
                    }

                    if (opcode.IsTerminating(_releaseSpec))
                    {
                        var expectedHeight = opcode is Instruction.RETF ? typesection[sectionId * MINIMUM_TYPESECTION_SIZE + 1] : stackHeight;
                        if (expectedHeight != stackHeight)
                        {
                            header = null;
                            return Failure<String>.From($"EIP-5450 : Stack state invalid required height {expectedHeight} but found {stackHeight}");
                        }
                        break;
                    }
                    
                    else if(pos >= code.Length)
                    {
                        header = null;
                        return Failure<String>.From($"EIP-5450 : Invalid code, reached end of code without a terminating instruction");
                    }
                }
            }

            if (ValidateReachableCode(sectionId, code, recordedStackHeight.Keys, in header) is Failure<String> failure)
            {
                header = null;
                return failure;
            }

            if (peakStackHeight != suggestedMaxHeight)
            {
                header = null;
                return Failure<String>.From($"EIP-5450 : Suggested Max Stack height mismatches with actual Max, expected {suggestedMaxHeight} but found {peakStackHeight}");
            }

            return peakStackHeight <= MAXIMUM_DATA_STACKHEIGHT 
                ? Success<bool>.From(true) 
                : Failure<String>.From($"EIP-5450 : Stack height exceeds maximum allowed {MAXIMUM_DATA_STACKHEIGHT}");
        }
    }
}
