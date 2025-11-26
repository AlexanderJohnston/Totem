using Totem.Commands;

namespace Totem.Mvc.Controllers;

public sealed class HttpCommandController<TCommand> : ControllerBase
    where TCommand : IHttpCommand
{
    readonly ILogger _logger;
    readonly ICommandPipeline _pipeline;

    public HttpCommandController(ILogger<HttpCommandController<TCommand>> logger, ICommandPipeline pipeline)
    {
        _logger = logger;
        _pipeline = pipeline;
    }

    [ErrorInfoActionFilter]
    public async Task<IActionResult> Handle([FromTotem] TCommand command, CancellationToken cancellationToken)
    {
        var envelope = new CommandEnvelope(command, principal: User);
        var commandType = envelope.CommandType;
        var commandId = envelope.CommandId;

        _logger.LogDebug("Run HTTP pipeline for {RequestMethod:l} {@CommandType:l}.{CommandId:l}", Request.Method, commandType, commandId);

        try
        {
            var context = await _pipeline.RunAsync(envelope, cancellationToken);

            context.ExpectNoErrors();

            return Ok();
        }
        catch(ErrorInfoException exception)
        {
            _logger.LogError(exception, "HTTP pipeline failed for {RequestMethod:l} {@CommandType:l}.{CommandId:l}", Request.Method, commandType, commandId);

            return new ErrorInfoResult(exception.Errors);
        }
    }
}
