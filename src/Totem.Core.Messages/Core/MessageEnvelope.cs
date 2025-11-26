namespace Totem.Core;

public abstract class MessageEnvelope
{
    protected MessageEnvelope(EnvelopeInfo info) =>
        Info = info;

    public EnvelopeInfo Info { get; }
    public Id CorrelationId => Info.CorrelationId;
    public ClaimsPrincipal Principal => Info.Principal;
}
