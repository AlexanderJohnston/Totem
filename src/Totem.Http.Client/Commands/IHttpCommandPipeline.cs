namespace Totem.Commands;

public interface IHttpCommandPipeline
{
    Task<IHttpCommandContext<IHttpCommand>> RunAsync(HttpCommandEnvelope envelope, CancellationToken cancellationToken);
}
