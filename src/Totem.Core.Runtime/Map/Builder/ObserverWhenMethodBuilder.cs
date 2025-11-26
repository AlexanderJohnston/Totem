namespace Totem.Map.Builder;

internal sealed class ObserverWhenMethodBuilder : RuntimeMethodBuilder<ObserverWhenMethod>
{
    readonly Type _extendedContextType;

    internal ObserverWhenMethodBuilder(MethodInfo method, Type extendedContextType) : base(method) =>
        _extendedContextType = extendedContextType;

    internal ObserverMethodParameterBuilder Parameter { get; private set; } = null!;

    internal override void Reflect(RuntimeMapBuilder map)
    {
        var parameters = Info.GetParameters();

        if(parameters.Length != 1)
        {
            Ignored = true;
            return;
        }

        Parameter = new ObserverMethodParameterBuilder(parameters[0], _extendedContextType);
        Parameter.Reflect(map);

        if(!Parameter.HasValue)
        {
            Ignored = true;
            return;
        }

        if(Info.Name != RuntimeMethod.When)
        {
            if(TypoDetector.IsPossibleTypo(RuntimeMethod.When, Info.Name))
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

        if(Info.IsStatic)
        {
            SetError(BuildErrors.RuntimeMethodStatic);
            return;
        }

        if(Info.ReturnType != typeof(void))
        {
            SetError(BuildErrors.ObserverWhenReturnTypeNotVoid);
        }
    }

    protected override ObserverWhenMethod BuildValue() =>
        new(Info, Parameter.Build());

    protected override RuntimeMapError? CollectParameterError() =>
        Parameter.CollectError();
}
