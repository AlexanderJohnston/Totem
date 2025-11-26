namespace Totem.Core;

public abstract class MessageContext : IMessageContext
{
    protected MessageContext(MessageEnvelope envelope) =>
        Envelope = envelope;

    public MessageEnvelope Envelope { get; }
    public EnvelopeInfo EnvelopeInfo => Envelope.Info;
    public Id CorrelationId => EnvelopeInfo.CorrelationId;
    public ClaimsPrincipal Principal => EnvelopeInfo.Principal;
    public ErrorBag Errors { get; } = new();
    public bool HasErrors => Errors.Any;

    public void AddError(ErrorInfo error) =>
        Errors.Add(error);

    public void AddErrors(IEnumerable<ErrorInfo> errors)
    {
        foreach(var error in errors)
        {
            Errors.Add(error);
        }
    }

    public void AddErrors(params ErrorInfo[] errors)
    {
        foreach(var error in errors)
        {
            Errors.Add(error);
        }
    }

    public void ExpectNoErrors() =>
        Errors.ExpectNone();
}
