
using TypeExtensions;
using Nethermind.Specs;
namespace Nethermind.Evm;
enum SectionDividor : byte
{
    Terminator = 0,
    CodeSection = 1,
    DataSection = 2,
    TypeSection = 3,
}
public class EofHeader
{
    #region public construction properties
    public int TypeSize { get; set; }
    public int[] CodeSize { get; set; }
    public int CodesSize => CodeSize?.Sum() ?? 0;
    public int DataSize { get; set; }
    public byte Version { get; set; }
    public int HeaderSize => 2 + 1 + (DataSize == 0 ? 0 : (1 + 2)) + (TypeSize == 0 ? 0 : (1 + 2)) + 3 * CodeSize.Length + 1;
    public int ContainerSize => TypeSize + CodesSize + DataSize;
    #endregion

    #region Equality methods
    public override bool Equals(object? obj)
        => this.GetHashCode() == obj.GetHashCode();
    public override int GetHashCode()
        => CodeSize.GetHashCode() ^ DataSize.GetHashCode() ^ TypeSize.GetHashCode();
    #endregion

    #region Sections Offsets
    public Range TypeSectionOffsets => new Range(HeaderSize, HeaderSize + TypeSize);
    public Range CodeSectionOffsets => new Range(TypeSectionOffsets.End, TypeSectionOffsets.End.Value + CodesSize);
    public Range DataSectionOffsets => new Range(CodeSectionOffsets.End, CodeSectionOffsets.End.Value + DataSize);
    public (int Start, int Size) this[int i] => (CodeSize.Take(i).Sum(), CodeSize[i]);
    #endregion

    public override string ToString() {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
}

public class EvmObjectFormat
{
    // magic prefix : EofFormatByte is the first byte, EofFormatDiff is chosen to diff from previously rejected contract according to EIP3541
    private const byte EofMagicLength = 2;
    private const byte EofFormatByte = 0xEF;
    private const byte EofFormatDiff = 0x00;
    private byte[] EofMagic => new byte[] { EofFormatByte, EofFormatDiff };

    public bool HasEofPrefix(ReadOnlySpan<byte> code, out byte? version) {
        version = null;
        if(code.Length > EofMagicLength && code.StartsWith(EofMagic)) {
            version = code[EofMagicLength];
            return true;
        }
        return false;
    } 
    public Result ExtractHeader(ReadOnlySpan<byte> code, IReleaseSpec spec)
    {
        if (!HasEofPrefix(code, out byte? version))
        {
            return Failure<String>.From($"Code doesn't start with Magic byte sequence expected 0xEF00");
        }

        int codeLen = code.Length;

        int i = EofMagicLength + 1;

        var header = new EofHeader
        {
            Version = version.Value
        };

        switch (version.Value)
        {
            case 1:
                return HandleEOF1(spec, code, ref header, codeLen, ref i);
            default:
                return Failure<String>.From($"Code has wrong EOFn version expected {1} but found {version.Value}");
        }
    }

    private Result HandleEOF1(IReleaseSpec spec, ReadOnlySpan<byte> code, ref EofHeader header, int codeLen, ref int i)
    {
        bool continueParsing = true;

        List<int> CodeSections = new();
        int? TypeSections = null;
        int? DataSections = null;

        while (i < codeLen && continueParsing)
        {
            var sectionKind = (SectionDividor)code[i];
            i++;

            switch (sectionKind)
            {
                case SectionDividor.Terminator:
                    {
                        if (CodeSections.Count == 0 || CodeSections.Any(sectionSize => sectionSize == 0 ))
                        {
                            return Failure<String>.From($"no CodeSection can have a 0 length, CodeSection length was {header.CodesSize}");
                        }

                        if (CodeSections.Count > 1 && CodeSections.Count != (TypeSections / 2))
                        {
                            return Failure<String>.From($"CodeSection count must match TypeSection count, CodeSection count was {CodeSections.Count}, expected {TypeSections / 2}");
                        }

                        if (CodeSections.Count > 1024)
                        {
                            return Failure<String>.From($"Code section count limit exceeded, only 1024 allowed but found {CodeSections.Count}");
                        }

                        header.CodeSize = CodeSections.ToArray();
                        header.TypeSize = TypeSections ?? 0;
                        header.DataSize = DataSections ?? 0;
                        continueParsing = false;
                        break;
                    }
                case SectionDividor.TypeSection:
                    {
                        if (spec.IsEip4750Enabled)
                        {
                            if (DataSections is not null || CodeSections.Count != 0)
                            {
                                return Failure<String>.From($"TypeSection must be before : CodeSection, DataSection");
                            }

                            if (TypeSections is not null)
                            {
                                return Failure<String>.From($"container must have at max 1 TypeSection but found more");
                            }

                            if (i + 2 > codeLen)
                            {
                                return Failure<String>.From($"type section code incomplete, failed parsing type section");
                            }

                            var typeSectionSize = code.Slice(i, 2).ReadEthInt16();
                            TypeSections = typeSectionSize;
                        }
                        else
                        {
                            return Failure<String>.From($"Encountered incorrect Section-Kind {sectionKind}, correct values are [{SectionDividor.CodeSection}, {SectionDividor.DataSection}, {SectionDividor.Terminator}]");
                        }
                        i += 2;
                        break;
                    }
                case SectionDividor.CodeSection:
                    {
                        if (i + 2 > codeLen)
                        {
                            return Failure<String>.From($"container code incomplete, failed parsing code section");
                        }

                        var codeSectionSize = code.Slice(i, 2).ReadEthInt16();
                        CodeSections.Add(codeSectionSize);

                        if (codeSectionSize == 0) // code section must be non-empty (i.e : size > 0)
                        {
                            return Failure<String>.From($"CodeSection size must be strictly bigger than 0 but found 0");
                        }

                        i += 2;
                        break;
                    }
                case SectionDividor.DataSection:
                    {
                        // data-section must come after code-section and there can be only one data-section
                        if (CodeSections.Count == 0)
                        {
                            return Failure<String>.From($"DataSection size must follow a CodeSection, CodeSection length was {header.CodeSize?[0] ?? 0}");
                        }
                        if (DataSections is not null)
                        {
                            return Failure<String>.From($"container must have at max 1 DataSection but found more");
                        }

                        if (i + 2 > codeLen)
                        {
                            return Failure<String>.From($"container code incomplete, failed parsing data section");
                        }

                        var dataSectionSize = code.Slice(i, 2).ReadEthInt16();
                        DataSections = dataSectionSize;

                        if (dataSectionSize == 0) // if declared data section must be non-empty
                        {
                            return Failure<String>.From($"DataSection size must be strictly bigger than 0 but found 0");
                        }

                        i += 2;
                        break;
                    }
                default: // if section kind is anything beside a section-limiter or a terminator byte we return false
                    {
                        return Failure<String>.From($"Encountered incorrect Section-Kind {sectionKind}, correct values are [{SectionDividor.TypeSection}, {SectionDividor.CodeSection}, {SectionDividor.DataSection}, {SectionDividor.Terminator}]");
                    }
            }
        }
        var contractBody = code[i..];

        var calculatedCodeLen = header.TypeSize + header.CodesSize + header.DataSize;
        if (spec.IsEip4750Enabled && header.TypeSize != 0 && contractBody.Length > 1 && contractBody[0] != 0 && contractBody[1] != 0)
        {
            return Failure<String>.From($"Invalid Type Section expected [{0}, {0}, ...] but found [{contractBody[0]}, {contractBody[1]}, ...]");
        }

        if (contractBody.Length == 0 || calculatedCodeLen != contractBody.Length)
        {
            return Failure<String>.From($"SectionSizes indicated in bundeled header are incorrect, or ContainerCode is incomplete");
        }
        return Success<EofHeader>.From(header);
    }

    public Result ValidateInstructions(ReadOnlySpan<byte> code, IReleaseSpec spec)
    {
        // check if code is EOF compliant
        if (!spec.IsEip3540Enabled)
        {
            return Failure<String>.From($"EOF is not enabled on this chain");
        }
        
        var result = ExtractHeader(code, spec);
        return result switch {
            Failure<String> error => error,
            Success<EofHeader> header => validateInstructionInit(ref code, header.Value, spec),
        };
    }

    Result validateInstructionInit(ref ReadOnlySpan<byte> code, EofHeader header, IReleaseSpec spec) {
        
        for (ushort i = 0; i < header.CodeSize.Length; i++)
        {
            var opcodeValidationResult = ValidateSectionInstructions(ref code, i, header, spec);
            if (opcodeValidationResult is Failure<String> failure1)
            {
                return Failure<String>.From(failure1.Message);
            }

            if (spec.IsEip5450Enabled)
            {
                var stackValidationResult = ValidateStackState(ref code, i, header, spec);
                if (opcodeValidationResult is Failure<String> failure2)
                {
                    return Failure<String>.From(failure2.Message);
                }
            }
        }
        return Success<EofHeader>.From(header);
    }
    private Result ValidateStackState(ref ReadOnlySpan<byte> container, ushort funcId, EofHeader header, IReleaseSpec spec)
    {
        Dictionary<int, int?> visitedLines = new();
        byte[] typesection = header.TypeSize != 0 
            ? container.Slice(header.ContainerSize, header.TypeSize).ToArray()
            : new byte[] {0 , 0};
        var (startOffset, sectionSize) = header[funcId];
        ReadOnlySpan<byte> code = container.Slice(header.CodeSectionOffsets.Start.Value + startOffset, sectionSize);

        int peakStackHeight = typesection[funcId * 2];
        Stack<(int Position, int StackHeigth)> workSet = new();
        workSet.Push((0, peakStackHeight));

        while (workSet.TryPop(out var worklet))
        {
            (int pos, int stackHeight) = worklet;
            bool stop = false;
            while (!stop)
            {
                Instruction opcode = (Instruction)code[pos];
                (var inputs, var immediates, var outputs) = opcode.StackRequirements(spec);

                if (visitedLines.ContainsKey(pos))
                {
                    if (stackHeight != visitedLines[pos])
                    {
                        return Failure<String>.From($"EIP-5450 : Branch joint line has invalid stack height");
                    }
                    break;
                }
                else
                {
                    visitedLines[pos] = stackHeight;
                }

                if (opcode is Instruction.CALLF or Instruction.JUMPF)
                {
                    var sectionIndex = code.Slice(pos + 1, 2).ReadEthUInt16();
                    inputs = typesection[sectionIndex * 2];
                    outputs = typesection[sectionIndex * 2 + 1];
                }

                if (stackHeight < inputs)
                {
                    return Failure<String>.From($"EIP-5450 : Stack Underflow required {inputs} but found {stackHeight}");
                }

                stackHeight += outputs - inputs;
                peakStackHeight = Math.Max(peakStackHeight, stackHeight);

                switch (opcode)
                {
                    case Instruction.RJUMP:
                        {
                            var offset = code.Slice(pos + 1, 2).ReadEthInt16();
                            var jumpDestination = pos + immediates + 1 + offset;
                            pos += jumpDestination;
                            break;
                        }
                    case Instruction.RJUMPI:
                        {
                            var offset = code.Slice(pos + 1, 2).ReadEthInt16();
                            var jumpDestination = pos + immediates + 1 + offset;
                            workSet.Push((jumpDestination, stackHeight));
                            pos += immediates + 1;
                            break;
                        }
                    case Instruction.RJUMPV:
                        {
                            var count = code[pos + 1];
                            immediates = count * 2 + 1;
                            for (short j = 0; j < count; j++)
                            {
                                int case_v = pos + 2 + j * 2;
                                int offset = code.Slice(case_v, 2).ReadEthInt16();
                                int jumptDestination = pos + immediates + 1 + offset;
                                workSet.Push((jumptDestination, stackHeight));
                            }
                            pos += immediates + 1;
                            break;
                        }
                    default:
                        {
                            if (opcode.IsTerminatingInstruction())
                            {
                                var expectedHeight = opcode is Instruction.RETF or Instruction.JUMPF ? typesection[funcId * 2 + 1] : 0;
                                if (expectedHeight != stackHeight)
                                {
                                    return Failure<String>.From($"EIP-5450 : Stack state invalid required height {expectedHeight} but found {stackHeight}");
                                }
                                stop = true;
                            }
                            else
                            {
                                pos += 1 + immediates;
                            }
                            break;
                        }
                }

            }

        }
        return peakStackHeight <= 1024 ? Success<bool>.From(true) : Failure<String>.From($"EIP-5450 : Stack state invalid peak height {peakStackHeight} exceeds 1024");
    }
    public Result ValidateSectionInstructions(ref ReadOnlySpan<byte> container, int sectionId, EofHeader header, IReleaseSpec spec)
    {
        // check if code is EOF compliant
        if (!spec.IsEip3540Enabled)
        {
            return Failure<String>.From($"EOF is not enabled on this chain");
        }

        if (!spec.IsEip3670Enabled)
        {
            return Success<bool>.From(true);
        }

        var (startOffset, sectionSize) = header[sectionId];
        ReadOnlySpan<byte> code = container.Slice(header.CodeSectionOffsets.Start.Value + startOffset, sectionSize);
        Instruction? opcode = null;
        HashSet<Range> immediates = new HashSet<Range>();
        HashSet<Int32> rjumpdests = new HashSet<Int32>();
        for (int i = 0; i < sectionSize;)
        {
            opcode = (Instruction)code[i];
            i++;
            // validate opcode
            if (!opcode.Value.IsValid(spec, true))
            {
                return Failure<String>.From($"CodeSection contains undefined opcode {opcode}");
            }

            if (spec.IsEip4200Enabled)
            {
                if (opcode is Instruction.RJUMP or Instruction.RJUMPI)
                {
                    if (i + 2 > sectionSize)
                    {
                        return Failure<String>.From($"Static Relative Jump Argument underflow");
                    }

                    var offset = code.Slice(i, 2).ReadEthInt16();
                    immediates.Add(new Range(i, i + 1));
                    var rjumpdest = offset + 2 + i;
                    rjumpdests.Add(rjumpdest);
                    if (rjumpdest < 0 || rjumpdest >= sectionSize)
                    {
                        return Failure<String>.From($"Static Relative Jump Destination outside of Code bounds");
                    }
                    i += 2;
                }
            }

            if (spec.IsEip4750Enabled)
            {
                if (opcode is Instruction.CALLF)
                {
                    if (i + 2 > sectionSize)
                    {
                        return Failure<String>.From($"CALLF Argument underflow");
                    }

                    var targetSectionId = code.Slice(i, 2).ReadEthUInt16();
                    immediates.Add(new Range(i, i + 1));

                    if (targetSectionId >= header.CodeSize.Length)
                    {
                        return Failure<String>.From($"invalid section id");
                    }
                    i += 2;
                }
            }

            if (opcode is >= Instruction.PUSH1 and <= Instruction.PUSH32)
            {
                int len = code[i - 1] - (int)Instruction.PUSH1 + 1;
                immediates.Add(new Range(i, i + len));
                i += len;
            }
        }

        if (spec.IsEip4200Enabled)
        {

            foreach (int rjumpdest in rjumpdests)
            {
                foreach (var range in immediates)
                {
                    if (range.Includes(rjumpdest))
                    {
                        return Failure<String>.From($"Static Relative Jump destination {rjumpdest} is an Invalid, falls within {range}");
                    }
                }
            }
        }
        return Success<bool>.From(true);
    }
}