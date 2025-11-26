namespace Totem.Core;

public sealed class TimelineKey : IEquatable<TimelineKey>
{
    public TimelineKey(Type declaredType, Id id)
    {
        DeclaredType = declaredType;
        Id = id;
    }

    public Type DeclaredType { get; }
    public Id Id { get; }

    public override string ToString() =>
        $"{DeclaredType.Name}.{Id}";

    public override int GetHashCode() =>
        HashCode.Combine(DeclaredType, Id);

    public override bool Equals(object? obj) =>
        obj is TimelineKey other && Equals(other);

    public bool Equals(TimelineKey? other) =>
        other is not null && DeclaredType == other.DeclaredType && Id == other.Id;

    public static bool operator ==(TimelineKey? x, TimelineKey? y) => EqualityComparer<TimelineKey?>.Default.Equals(x, y);
    public static bool operator !=(TimelineKey x, TimelineKey? y) => !(x == y);
}
