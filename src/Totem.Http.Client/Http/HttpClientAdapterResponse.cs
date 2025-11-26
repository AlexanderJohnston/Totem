namespace Totem.Http;

public sealed class HttpClientAdapterResponse
{
    HttpClientAdapterResponse(HttpResponseMessage message, string content)
    {
        Message = message;
        StatusCode = message.StatusCode;
        IsSuccessStatus = message.IsSuccessStatusCode;
        ReasonPhrase = message.ReasonPhrase;
        Headers = message.Headers;
        ContentType = message.Content.Headers.ContentType?.MediaType?.ToString() ?? ContentTypes.PlainText;
        Content = content;
    }

    public HttpResponseMessage Message { get; }
    public HttpStatusCode StatusCode { get; }
    public bool IsSuccessStatus { get; }
    public string? ReasonPhrase { get; }
    public HttpResponseHeaders Headers { get; }
    public string ContentType { get; }
    public string Content { get; }

    public override string ToString() =>
        $"{StatusCode} {ReasonPhrase}";

    public static async Task<HttpClientAdapterResponse> CreateAsync(HttpResponseMessage message, CancellationToken cancellationToken) =>
        new (message, await message.Content.ReadAsStringAsync(cancellationToken));
}
