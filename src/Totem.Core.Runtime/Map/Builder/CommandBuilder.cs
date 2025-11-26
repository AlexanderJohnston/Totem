namespace Totem.Map.Builder;

internal sealed class CommandBuilder : RuntimeTypeBuilder<CommandType>
{
    internal CommandBuilder(Type declaredType) : base(declaredType)
    { }

    internal CommandInfo Info { get; private set; } = null!;
    internal TopicBuilder Topic { get; set; } = null!;
    internal TopicRouteMethodBuilder? Route { get; set; }
    internal TopicWhenMethodBuilder When { get; set; } = null!;

    internal override void Reflect(RuntimeMapBuilder map)
    {
        if(!CommandInfo.TryFrom(DeclaredType, out var info))
        {
            SetError(BuildErrors.MessageInfoNotFound);
            return;
        }

        Info = info;
    }

    internal override void Validate()
    {
        if(Topic is null)
        {
            SetError(BuildErrors.TopicNotFound);
            return;
        }
    }

    protected override CommandType BuildValue() =>
        new(Info);

    internal void BuildMethodValues()
    {
        Value.Route = Route?.Build();
        Value.When = When.Build();
    }
}
