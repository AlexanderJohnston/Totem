namespace Totem.Commands;

public interface ICommandPipeline
{
    Task<ICommandContext<ICommand>> RunAsync(CommandEnvelope envelope, CancellationToken cancellationToken);
}
