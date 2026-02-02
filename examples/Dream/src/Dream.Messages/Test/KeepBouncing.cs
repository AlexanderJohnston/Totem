public sealed class KeepBouncing : IWorkflowCommand
{
    public KeepBouncing(Id bounceId, int test)
    {
        Test = test;
        BounceId = bounceId;
    }

    public int Test { get; }
    public Id BounceId { get; }
}
