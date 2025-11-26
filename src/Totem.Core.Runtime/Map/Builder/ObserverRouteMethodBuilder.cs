namespace Totem.Map.Builder;

internal sealed class ObserverRouteMethodBuilder : RuntimeMethodBuilder<ObserverRouteMethod>
{
    internal ObserverRouteMethodBuilder(MethodInfo method) : base(method)
    { }

    internal ObserverMethodParameterBuilder Parameter { get; private set; } = null!;
    internal bool ReturnsMany { get; private set; }

    internal override void Reflect(RuntimeMapBuilder map)
    {
        var parameters = Info.GetParameters();

        if(parameters.Length != 1)
        {
            Ignored = true;
            return;
        }

        Parameter = new ObserverMethodParameterBuilder(parameters[0]);
        Parameter.Reflect(map);

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

        ReturnsMany = typeof(IEnumerable<Id>).IsAssignableFrom(Info.ReturnType);

        if(!ReturnsMany && Info.ReturnType != typeof(Id))
        {
            SetError(BuildErrors.ObserverRouteReturnTypeNotIdOrIdSequence);
            return;
        }
    }

    protected override ObserverRouteMethod BuildValue() =>
        new(Info, Parameter.Build(), ReturnsMany);

    protected override RuntimeMapError? CollectParameterError() =>
        Parameter.CollectError();
}
