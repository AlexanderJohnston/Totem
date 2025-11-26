namespace Totem.Commands;

public interface IHttpCommandNegotiator
{
    HttpRequestMessage Negotiate(IHttpCommandContext<IHttpCommand> context);
}
