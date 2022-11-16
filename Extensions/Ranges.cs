
namespace TypeExtensions;
public static class RangeExtensions
{
    public static bool Includes(this Range @this, int value)
        => value >= @this.Start.Value && value <= @this.End.Value;

    public static bool Includes(this Range @this, int value, int len)
    {
        var (offset, length) = @this.GetOffsetAndLength(len);
        return value >= offset && value < length + offset;
    }
    public static bool Intersects(this Range @this, Range other)
        => @this.Start.Value <= other.End.Value && other.Start.Value <= @this.End.Value;

}