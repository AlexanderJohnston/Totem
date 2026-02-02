using Dream.Test;

namespace Dream.Versions.Topics;

public sealed class EchoBounceTopic : Topic
{
    public static Id Route(KeepBouncing command) => command.BounceId;

    public async Task When(KeepBouncing command, CancellationToken cancellationToken)
    {
        Then(new DoneBouncing(command.BounceId, command.Test));
    }
}
