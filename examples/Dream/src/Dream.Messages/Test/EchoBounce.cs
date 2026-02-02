namespace Dream.Test;

public sealed class EchoBounce : IHttpCommand
{
    public EchoBounce(int test) =>
        Test = test;

    public int Test { get; }
}
