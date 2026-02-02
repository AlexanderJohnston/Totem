namespace Dream.Versions;
public sealed class DoneBouncing : IEvent
{
    public DoneBouncing(Id id, int test)
    {
        Test = test;
        BounceId = id;
    }

    public int Test { get; }
    public Id BounceId { get; }
}
