using Dream.Test;

namespace Dream.Versions.Topics;

public sealed class BounceWorkflow : Workflow
{
    public static Id Route(EchoBounced e) => e.BounceId;

    public void When(EchoBounced e) =>
        ThenEnqueue(new KeepBouncing(e.BounceId, e.Test));
}
