namespace Totem.Map.Builder;

internal sealed class WorkflowBuilder : ObserverBuilder<WorkflowType>
{
    internal WorkflowBuilder(Type declaredType) : base(declaredType)
    { }

    protected override Type ExtendedContextType => typeof(IWorkflowContext<>);

    protected override WorkflowType BuildValue()
    {
        var workflow = new WorkflowType(DeclaredType, IsSingleInstance);

        foreach(var builder in Observations)
        {
            if(builder.HasError)
            {
                continue;
            }

            var observation = builder.Build();

            workflow.Observations.Add(observation);

            observation.Observer = workflow;
            observation.Event.WorkflowObservations.Add(observation);
        }

        return workflow;
    }
}
