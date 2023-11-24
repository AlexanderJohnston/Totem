namespace Totem.Core;

public struct TimelinePosition : IEquatable<TimelinePosition>, IComparable<TimelinePosition>
{
    public static readonly TimelinePosition Start = new(null);

    readonly long? _index;

    TimelinePosition(long? index) =>
        _index = index;

    public bool IsStart => _index is null;

    public long ToIndex() =>
        _index ?? throw new Exception("Expected position to be beyond timeline start");

    public override string ToString() =>
        _index?.ToString() ?? "--";

    public override int GetHashCode() =>
        _index.GetHashCode();

    public override bool Equals(object? obj) =>
        obj is TimelinePosition other && Equals(other);

    public bool Equals(TimelinePosition other) =>
        _index == other._index;

    public int CompareTo(TimelinePosition other) =>
        Comparer<long?>.Default.Compare(_index, other._index);

    public TimelinePosition Next() =>
        _index is null ? new(0) : new(_index + 1);

    public static bool TryFrom(long index, out TimelinePosition position)
    {
        if(index >= 0)
        {
            position = new(index);
            return true;
        }

        position = default;
        return false;
    }

    public static TimelinePosition From(long index)
    {
        if(!TryFrom(index, out var position))
            throw new FormatException($"Expected a non-negative index: {index}");

        return position;
    }

    public static bool operator ==(TimelinePosition x, TimelinePosition y) => EqualityComparer<TimelinePosition>.Default.Equals(x, y);
    public static bool operator !=(TimelinePosition x, TimelinePosition y) => !(x == y);
    public static bool operator <(TimelinePosition x, TimelinePosition y) => Comparer<TimelinePosition>.Default.Compare(x, y) < 0;
    public static bool operator >(TimelinePosition x, TimelinePosition y) => Comparer<TimelinePosition>.Default.Compare(x, y) > 0;
    public static bool operator <=(TimelinePosition x, TimelinePosition y) => Comparer<TimelinePosition>.Default.Compare(x, y) <= 0;
    public static bool operator >=(TimelinePosition x, TimelinePosition y) => Comparer<TimelinePosition>.Default.Compare(x, y) >= 0;

    public static explicit operator TimelinePosition(long v)
    {
        TimelinePosition position;
        var tried = TryFrom(v, out position);
        if(tried)
        {
            return position;
        }
        else
        {
            throw new InvalidCastException($"Expected a non-negative index when converting to TimelinePosition: {v}");
        }
    }
}
