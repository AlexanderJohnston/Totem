namespace Totem.Core;

public sealed class EnvelopeInfo
{
    public EnvelopeInfo(Id messageId, Id? correlationId = null, ClaimsPrincipal? principal = null)
    {
        MessageId = messageId;
        CorrelationId = correlationId ?? Id.NewId();
        Principal = principal ?? new ClaimsPrincipal(new ClaimsIdentity());
    }

    public Id MessageId { get; }
    public Id CorrelationId { get; }
    public ClaimsPrincipal Principal { get; }

    public override string ToString() =>
        MessageId.ToShortString();
}
