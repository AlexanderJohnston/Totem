namespace Totem.Map.Builder;

internal sealed class TopicGivenMethodBuilder : RuntimeMethodBuilder<TopicGivenMethod>
{
    internal TopicGivenMethodBuilder(MethodInfo method) : base(method)
    { }

    internal EventParameterBuilder Parameter { get; private set; } = null!;

    internal override void Reflect(RuntimeMapBuilder map)
    {
        var parameters = Info.GetParameters();

        if(parameters.Length != 1)
        {
            Ignored = true;
            return;
        }

        Parameter = new EventParameterBuilder(parameters[0]);
        Parameter.Reflect(map);

        if(!Parameter.HasValue)
        {
            Ignored = true;
            return;
        }

        if(Info.Name != RuntimeMethod.Given)
        {
            if(TypoDetector.IsPossibleTypo(RuntimeMethod.Given, Info.Name))
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
            SetError(BuildErrors.GivenReturnTypeNotVoid);
        }
    }

    protected override TopicGivenMethod BuildValue() =>
        new(Info, Parameter.Build());

    protected override RuntimeMapError? CollectParameterError() =>
        Parameter.CollectError();
}
