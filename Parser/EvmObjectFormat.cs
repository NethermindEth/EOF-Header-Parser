
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
    public (int Start, int Size) TypeSectionOffsets => (HeaderSize, TypeSize);
    public (int Start, int Size) CodeSectionOffsets => (HeaderSize + TypeSize, CodesSize);
    public (int Start, int Size) DataSectionOffsets => (HeaderSize + TypeSize + CodesSize, DataSize);
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

    public bool HasEOFFormat(ReadOnlySpan<byte> code) => code.Length > EofMagicLength && code.StartsWith(EofMagic);
    public Result<EofHeader, string> ExtractHeader(ReadOnlySpan<byte> code, IReleaseSpec spec)
    {
        if (!HasEOFFormat(code))
        {
            return Result<EofHeader, string>.Failure($"Code doesn't start with Magic byte sequence expected 0xEF00");
        }

        int codeLen = code.Length;

        int i = EofMagicLength;
        byte EOFVersion = code[i++];

        var header = new EofHeader
        {
            Version = EOFVersion
        };

        switch (EOFVersion)
        {
            case 1:
                return HandleEOF1(spec, code, ref header, codeLen, ref i);
            default:
                return Result<EofHeader, string>.Failure($"Code has wrong EOFn version expected {1} but found {EOFVersion}");
        }
    }

    private Result<EofHeader, string> HandleEOF1(IReleaseSpec spec, ReadOnlySpan<byte> code, ref EofHeader header, int codeLen, ref int i)
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
                        if (CodeSections.Count == 0 || CodeSections[0] == 0)
                        {
                            return Result<EofHeader, string>.Failure($"CodeSection size must follow a CodeSection, CodeSection length was {header.CodesSize}");
                        }

                        if (CodeSections.Count > 1 && CodeSections.Count != (TypeSections / 2))
                        {
                            return Result<EofHeader, string>.Failure($"CodeSection count must match TypeSection count, CodeSection count was {CodeSections.Count}, expected {TypeSections / 2}");
                        }

                        if (CodeSections.Count > 1024)
                        {
                            return Result<EofHeader, string>.Failure($"Code section count limit exceeded, only 1024 allowed but found {CodeSections.Count}");
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
                                return Result<EofHeader, string>.Failure($"TypeSection must be before : CodeSection, DataSection");
                            }

                            if (TypeSections is not null)
                            {
                                return Result<EofHeader, string>.Failure($"container must have at max 1 TypeSection but found more");
                            }

                            if (i + 2 > codeLen)
                            {
                                return Result<EofHeader, string>.Failure($"type section code incomplete, failed parsing type section");
                            }

                            var typeSectionSize = code.Slice(i, 2).ReadEthInt16();
                            TypeSections = typeSectionSize;
                        }
                        else
                        {
                            return Result<EofHeader, string>.Failure($"Encountered incorrect Section-Kind {sectionKind}, correct values are [{SectionDividor.CodeSection}, {SectionDividor.DataSection}, {SectionDividor.Terminator}]");
                        }
                        i += 2;
                        break;
                    }
                case SectionDividor.CodeSection:
                    {
                        if (i + 2 > codeLen)
                        {
                            return Result<EofHeader, string>.Failure($"container code incomplete, failed parsing code section");
                        }

                        var codeSectionSize = code.Slice(i, 2).ReadEthInt16();
                        CodeSections.Add(codeSectionSize);

                        if (codeSectionSize == 0) // code section must be non-empty (i.e : size > 0)
                        {
                            return Result<EofHeader, string>.Failure($"CodeSection size must be strictly bigger than 0 but found 0");
                        }

                        i += 2;
                        break;
                    }
                case SectionDividor.DataSection:
                    {
                        // data-section must come after code-section and there can be only one data-section
                        if (CodeSections.Count == 0)
                        {
                            return Result<EofHeader, string>.Failure($"DataSection size must follow a CodeSection, CodeSection length was {header.CodeSize?[0] ?? 0}");
                        }
                        if (DataSections is not null)
                        {
                            return Result<EofHeader, string>.Failure($"container must have at max 1 DataSection but found more");
                        }

                        if (i + 2 > codeLen)
                        {
                            return Result<EofHeader, string>.Failure($"container code incomplete, failed parsing data section");
                        }

                        var dataSectionSize = code.Slice(i, 2).ReadEthInt16();
                        DataSections = dataSectionSize;

                        if (dataSectionSize == 0) // if declared data section must be non-empty
                        {
                            return Result<EofHeader, string>.Failure($"DataSection size must be strictly bigger than 0 but found 0");
                        }

                        i += 2;
                        break;
                    }
                default: // if section kind is anything beside a section-limiter or a terminator byte we return false
                    {
                        return Result<EofHeader, string>.Failure($"Encountered incorrect Section-Kind {sectionKind}, correct values are [{SectionDividor.TypeSection}, {SectionDividor.CodeSection}, {SectionDividor.DataSection}, {SectionDividor.Terminator}]");
                    }
            }
        }
        var contractBody = code[i..];

        var calculatedCodeLen = header.TypeSize + header.CodesSize + header.DataSize;
        if (spec.IsEip4750Enabled && header.TypeSize != 0 && contractBody.Length > 1 && contractBody[0] != 0 && contractBody[1] != 0)
        {
            return Result<EofHeader, string>.Failure($"Invalid Type Section expected [{0}, {0}, ...] but found [{contractBody[0]}, {contractBody[1]}, ...]");
        }

        if (contractBody.Length == 0 || calculatedCodeLen != contractBody.Length)
        {
            return Result<EofHeader, string>.Failure($"SectionSizes indicated in bundeled header are incorrect, or ContainerCode is incomplete");
        }
        return Result<EofHeader, string>.Success(header);
    }

    public Result<EofHeader?, string> ValidateEofCode(ReadOnlySpan<byte> code, IReleaseSpec spec) => ExtractHeader(code, spec);
    public Result<EofHeader?, string> ValidateInstructions(ReadOnlySpan<byte> code, IReleaseSpec spec)
    {
        // check if code is EOF compliant
        if (!spec.IsEip3540Enabled)
        {
            return Result<EofHeader?, string>.Failure($"EOF is not enabled on this chain");
        }

        var (header, headerError) = ExtractHeader(code, spec);
        if (header is not null)
        {
            bool valid = true;
            string validationError = string.Empty;
            for (int i = 0; i < header.CodeSize.Length; i++)
            {
                (var isValid, validationError) = ValidateSectionInstructions(ref code, i, header, spec);
                valid &= isValid is not null;
            }
            if(valid) {
                return Result<EofHeader, string>.Success(header);
            } else {
                return Result<EofHeader, string>.Failure(validationError);
            }
        }
        return Result<EofHeader, string>.Failure(headerError);
    }

    public Result<bool?, string?> ValidateSectionInstructions(ref ReadOnlySpan<byte> container, int sectionId, EofHeader header, IReleaseSpec spec)
    {
        // check if code is EOF compliant
        if (!spec.IsEip3540Enabled)
        {
            return Result<bool?, string>.Failure($"EOF is not enabled on this chain");
        }

        if (!spec.IsEip3670Enabled)
        {
            return Result<bool?, string>.Success(true);
        }

        var (startOffset, sectionSize) = header[sectionId];
        ReadOnlySpan<byte> code = container.Slice(header.CodeSectionOffsets.Start + startOffset, sectionSize);
        Instruction? opcode = null;
        HashSet<Range> immediates = new HashSet<Range>();
        HashSet<Int32> rjumpdests = new HashSet<Int32>();
        for (int i = 0; i < sectionSize;)
        {
            opcode = (Instruction)code[i];
            i++;
            // validate opcode
            if (!Enum.IsDefined(opcode.Value))
            {
                return Result<bool?, string>.Failure($"CodeSection contains undefined opcode {opcode}");
            }

            if (spec.IsEip4200Enabled)
            {
                if (opcode is Instruction.RJUMP or Instruction.RJUMPI)
                {
                    if (i + 2 > sectionSize)
                    {
                        return Result<bool?, string>.Failure($"Static Relative Jump Argument underflow");
                    }

                    var offset = code.Slice(i, 2).ReadEthInt16();
                    immediates.Add(new Range(i, i + 1));
                    var rjumpdest = offset + 2 + i;
                    rjumpdests.Add(rjumpdest);
                    if (rjumpdest < 0 || rjumpdest >= sectionSize)
                    {
                        return Result<bool?, string>.Failure($"Static Relative Jump Destination outside of Code bounds");
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
                        return Result<bool?, string>.Failure($"CALLF Argument underflow");
                    }

                    var targetSectionId = code.Slice(i, 2).ReadEthUInt16();
                    immediates.Add(new Range(i, i + 1));

                    if (targetSectionId >= header.CodeSize.Length)
                    {
                        return Result<bool?, string>.Failure($"invalid section id");
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

        bool endCorrectly = opcode switch
        {
            Instruction.RETF when spec.IsEip4750Enabled => true,
            Instruction.STOP or Instruction.RETURN or Instruction.REVERT or Instruction.INVALID or Instruction.SELFDESTRUCT
                => true,
            _ => false
        };

        if (!endCorrectly)
        {
            return Result<bool?, string>.Failure($"Last opcode {opcode} in CodeSection should be either [{Instruction.RETF}, {Instruction.STOP}, {Instruction.RETURN}, {Instruction.REVERT}, {Instruction.INVALID}, {Instruction.SELFDESTRUCT}");
        }

        if (spec.IsEip4200Enabled)
        {

            foreach (int rjumpdest in rjumpdests)
            {
                foreach (var range in immediates)
                {
                    if (range.Includes(rjumpdest))
                    {
                        return Result<bool?, string>.Failure($"Static Relative Jump destination {rjumpdest} is an Invalid, falls within {range}");
                    }
                }
            }
        }
        return Result<bool?, string>.Success(true);
    }
}