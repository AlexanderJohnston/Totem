namespace Totem.Map.Builder;

internal sealed class TopicRouteMethodBuilder : RuntimeMethodBuilder<TopicRouteMethod>
{
    internal TopicRouteMethodBuilder(MethodInfo method) : base(method)
    { }

    internal TopicRouteMethodParameterBuilder Parameter { get; private set; } = null!;

    internal override void Reflect(RuntimeMapBuilder map)
    {
        var parameters = Info.GetParameters();

        if(parameters.Length != 1)
        {
            Ignored = true;
            return;
        }

        Parameter = new TopicRouteMethodParameterBuilder(parameters[0]);
        Parameter.Reflect(map);

        if(!Parameter.HasValue)
        {
            Ignored = true;
            return;
        }

        if(Info.Name != RuntimeMethod.Route)
        {
            if(TypoDetector.IsPossibleTypo(RuntimeMethod.Route, Info.Name))
            {
                SetError(BuildErrors.RuntimeMethodPossibleTypo, new { Info.Name });
            }
            else
            {
                Ignored = true;
            }

            return;
        }

        if(!Info.IsPublic)
        {
            SetError(BuildErrors.RuntimeMethodNotPublic);
            return;
        }

        if(!Info.IsStatic)
        {
            SetError(BuildErrors.RuntimeMethodNotStatic);
            return;
        }

        if(Info.ReturnType != typeof(Id))
        {
            SetError(BuildErrors.CommandRouteReturnTypeNotId);
        }
    }

    protected override TopicRouteMethod BuildValue() =>
        new(Info, Parameter.Build());

    protected override RuntimeMapError? CollectParameterError() =>
        Parameter.CollectError();
}
