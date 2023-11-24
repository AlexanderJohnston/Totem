namespace Totem.Core;

public interface IMessageContext
{
    MessageEnvelope Envelope { get; }
    EnvelopeInfo EnvelopeInfo { get; }
    Id CorrelationId { get; }
    ClaimsPrincipal Principal { get; }
    ErrorBag Errors { get; }
    bool HasErrors { get; }

    void AddError(ErrorInfo error);
    void AddErrors(IEnumerable<ErrorInfo> errors);
    void AddErrors(params ErrorInfo[] errors);
    void ExpectNoErrors();
}
