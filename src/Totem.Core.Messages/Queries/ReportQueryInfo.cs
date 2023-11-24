namespace Totem.Queries;

public sealed class ReportQueryInfo : MessageInfo
{
    static readonly MessageInfoCache<ReportQueryInfo> _cache = new();

    ReportQueryInfo(Type declaredType, ReportRowInfo row) : base(declaredType) =>
        Row = row;

    public ReportRowInfo Row { get; }

    public static bool TryFrom(Type type, [NotNullWhen(true)] out ReportQueryInfo? info)
    {
        if(_cache.TryGetValue(type, out info))
        {
            return true;
        }

        if(type is not null && type.IsConcreteClass() && typeof(IReportQuery).IsAssignableFrom(type))
        {
            var rowType = type.GetImplementedInterfaceGenericArguments(typeof(IReportQuery<>)).SingleOrDefault();

            if(rowType is not null && ReportRowInfo.TryFrom(rowType, out var row))
            {
                info = new ReportQueryInfo(type, row);

                _cache.Add(info);

                return true;
            }
        }

        return false;
    }

    public static ReportQueryInfo From(Type type)
    {
        if(!TryFrom(type, out var info))
            throw new ArgumentException($"Expected type to be a public, non-abstract class implementing {typeof(IReportQuery<>)}", nameof(type));

        return info;
    }
}
