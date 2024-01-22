using Realizer.Messages.Conversations;
using REBL;
using REBL.Tests;

namespace Realize.Web.Services;

public class RebelService
{
    private REBLConsole _console;
    public RebelService()
    {
        _console = new REBLConsole();
    }

    public string GetInput(string input)
    {
        var command = TesterHelper.GetInput(ref _console, input);
        return _console.RunHeadless(command).Result;
    }

    public string MakeTemplate(string templateName, string template)
    {
        var command = TesterHelper.MakeTemplate(ref _console, templateName, template);
        return _console.RunHeadless(command).Result;
    }

    public string MakeBuffer(string bufferName)
    {
        var command = TesterHelper.MakeBuffer(ref _console, bufferName);
        return _console.RunHeadless(command).Result;
    }

    public string AddTemplate(string templateName, string bufferName)
    {
        var command = TesterHelper.AddTemplate(ref _console, bufferName, templateName);
        return _console.RunHeadless(command).Result;
    }

    public string AddExpression(string expressionName, string bufferName)
    {
        var command = TesterHelper.AddExpression(ref _console, bufferName, expressionName);
        return _console.RunHeadless(command).Result;
    }

    public string MakeClaim(string bufferName)
    {
        var command = TesterHelper.MakeCLaim(ref _console, bufferName);
        return _console.RunHeadless(command).Result;
    }
}
