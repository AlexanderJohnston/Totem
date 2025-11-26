namespace Totem.Map;

public abstract class TimelineType : RuntimeType
{
    static readonly Id _singleInstanceNamespace = (Id) "adae821d-c9bd-4c56-b3c3-aab90324a2e1";

    internal TimelineType(Type declaredType, bool isSingleInstance) : base(declaredType)
    {
        if(isSingleInstance)
        {
            SingleInstanceId = _singleInstanceNamespace.DeriveId(declaredType.FullName ?? "");
        }
    }

    public Id? SingleInstanceId { get; private set; }
    [MemberNotNullWhen(true, nameof(SingleInstanceId))]
    public bool IsSingleInstance => SingleInstanceId is not null;
}
