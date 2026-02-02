namespace Dream.Test;
public sealed class EchoBounced : IEvent
{
    public EchoBounced(Id id, int test)
    {
        BounceId = id;
        Test = test;
    }

    public int Test { get; }
    public Id BounceId { get; }
}
