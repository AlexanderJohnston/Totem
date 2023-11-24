namespace Totem.Queries;

public sealed class ReportListQueryInfo : MessageInfo
{
    static readonly MessageInfoCache<ReportListQueryInfo> _cache = new();

    ReportListQueryInfo(Type declaredType, ReportRowInfo row) : base(declaredType) =>
        Row = row;

    public ReportRowInfo Row { get; }

    public static bool TryFrom(Type type, [NotNullWhen(true)] out ReportListQueryInfo? info)
    {
        if(_cache.TryGetValue(type, out info))
        {
            return true;
        }

        if(type is not null && type.IsConcreteClass() && typeof(IReportListQuery).IsAssignableFrom(type))
        {
            var rowType = type.GetImplementedInterfaceGenericArguments(typeof(IReportListQuery<>)).Single();

            if(ReportRowInfo.TryFrom(rowType, out var row))
            {
                info = new ReportListQueryInfo(type, row);

                _cache.Add(info);

                return true;
            }
        }

        return false;
    }

    public static ReportListQueryInfo From(Type type)
    {
        if(!TryFrom(type, out var info))
            throw new ArgumentException($"Expected type to be a public, non-abstract class implementing {typeof(IReportListQuery<>)}", nameof(type));

        return info;
    }
}
