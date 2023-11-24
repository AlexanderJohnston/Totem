namespace Totem.Core;

public sealed class TimelineVersion : IEquatable<TimelineVersion>
{
    public TimelineVersion(Id timelineId, TimelinePosition position)
    {
        TimelineId = timelineId;
        Position = position;
    }

    public TimelineVersion(Id timelineId) : this(timelineId, TimelinePosition.Start)
    { }

    public Id TimelineId { get; }
    public TimelinePosition Position { get; }
    public bool IsStart => Position.IsStart;

    public override string ToString() =>
        $"{TimelineId.ToShortString()}@{Position}";

    public override bool Equals(object? obj) =>
        obj is TimelineVersion other && Equals(other);

    public bool Equals(TimelineVersion? other) =>
        other is not null
        && TimelineId == other.TimelineId
        && Position == other.Position;

    public override int GetHashCode() =>
        HashCode.Combine(TimelineId, Position);

    public static bool operator ==(TimelineVersion? x, TimelineVersion? y) => EqualityComparer<TimelineVersion?>.Default.Equals(x, y);
    public static bool operator !=(TimelineVersion? x, TimelineVersion? y) => !(x == y);

    public static readonly TimelineVersion EmptyList = new(
        (Id) "92c0fda5-4f75-4792-9d90-581c895f40b4",
        TimelinePosition.Start);
}
