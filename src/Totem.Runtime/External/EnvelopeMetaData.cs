using Totem.Core;
using System.Security.Claims;

namespace Totem.External;

public class EnvelopeMetaData
{
    public EnvelopeMetaData(ItemKey messageKey, Id correlationId, Type declaredType, ClaimsPrincipal principal, DateTimeOffset whenOccurred)
    {
        MessageKey = messageKey;
        CorrelationId = correlationId;
        Principal = principal;
        EventType = declaredType;
        WhenOccurred = whenOccurred;
    }

    public Type EventType { get; }

    // This is just the Event Type
    public ItemKey MessageKey { get; }
    public Id CorrelationId { get; }
    // To one value e.g. Token
    public ClaimsPrincipal Principal { get; }
    public DateTimeOffset WhenOccurred { get; }
}
