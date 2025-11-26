namespace Totem.Reports;

public sealed class ReportRowInfo
{
    static readonly ConcurrentDictionary<Type, ReportRowInfo> _cache = new();

    ReportRowInfo(Type declaredType)
    {
        DeclaredType = declaredType;
        ExternalType = declaredType.GetCustomAttribute<ExternalTypeAttribute>()?.ExternalType ?? declaredType.Name;
    }

    public Type DeclaredType { get; }
    public string ExternalType { get; }

    public override string ToString() =>
        ExternalType;

    public static bool TryFrom(Type type, [NotNullWhen(true)] out ReportRowInfo? info)
    {
        if(_cache.TryGetValue(type, out info))
        {
            return true;
        }

        if(type is not null && type.IsConcreteClass() && typeof(IReportRow).IsAssignableFrom(type))
        {
            info = new ReportRowInfo(type);

            _cache[type] = info;

            return true;
        }

        return false;
    }

    public static ReportRowInfo From(Type type)
    {
        if(!TryFrom(type, out var info))
            throw new ArgumentException($"Expected type to be a public, non-abstract class implementing {typeof(IReportRow)}", nameof(type));

        return info;
    }
}
