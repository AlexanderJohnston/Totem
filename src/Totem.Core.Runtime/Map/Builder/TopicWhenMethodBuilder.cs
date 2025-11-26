namespace Totem.Map.Builder;

internal sealed class TopicWhenMethodBuilder : RuntimeMethodBuilder<TopicWhenMethod>
{
    internal TopicWhenMethodBuilder(MethodInfo method) : base(method)
    { }

    internal TopicWhenMethodParameterBuilder Parameter { get; private set; } = null!;
    internal bool IsAsync { get; private set; }
    internal bool HasCancellationToken { get; private set; }

    internal override void Reflect(RuntimeMapBuilder map)
    {
        var parameters = Info.GetParameters();

        if(parameters.Length == 0)
        {
            Ignored = true;
            return;
        }

        Parameter = new TopicWhenMethodParameterBuilder(parameters[0]);
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

        if(Info.ReturnType == typeof(void))
        {
            if(parameters.Length != 1)
            {
                SetError(BuildErrors.CommandWhenNotSingleParameter);
                return;
            }
        }
        else if(Info.ReturnType != typeof(Task))
        {
            SetError(BuildErrors.CommandWhenReturnTypeNotVoidOrTask);
            return;
        }
        else
        {
            IsAsync = true;

            switch(parameters.Length)
            {
                case 1:
                    break;
                case 2:
                    if(parameters[1].ParameterType != typeof(CancellationToken))
                    {
                        SetError(BuildErrors.CommandWhenSecondParameterNotCancellationToken);
                        return;
                    }

                    HasCancellationToken = true;
                    break;
                default:
                    SetError(BuildErrors.CommandWhenNotTwoParameters);
                    return;
            }
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
    }

    protected override TopicWhenMethod BuildValue() =>
        new(Info, Parameter.Build(), IsAsync, HasCancellationToken);

    protected override RuntimeMapError? CollectParameterError() =>
        Parameter.CollectError();
}
