namespace Totem.Subscriptions;

internal sealed class TspServerSubscriptionReference : ISubscriptionReference
{
    readonly TspServerHost _host;
    readonly SubscriptionAddress _address;
    readonly ISubscriptionReference _coreReference;

    internal TspServerSubscriptionReference(TspServerHost host, SubscriptionAddress address, ISubscriptionReference coreReference)
    {
        _host = host;
        _address = address;
        _coreReference = coreReference;
    }

    public async Task UnsubscribeAsync(CancellationToken cancellationToken)
    {
        _host.RemoveSubscription(_address);

        await _coreReference.UnsubscribeAsync(cancellationToken);
    }
}
