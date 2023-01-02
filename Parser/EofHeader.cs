namespace Nethermind.Evm.EOF;

public record struct EofHeader(byte Version,
    SectionHeader TypeSection,
    SectionHeader[] CodeSections,
    SectionHeader DataSection) {
    public override string ToString() => $"Version: {Version}, TypeSection: {TypeSection.Size}, CodeSections: {String.Join(',', CodeSections.Select(sec => sec.Size.ToString()))}, DataSection: {DataSection.Size}";
}

public record struct SectionHeader(int Start, ushort Size)
{
    public int EndOffset => Start + Size;
}
