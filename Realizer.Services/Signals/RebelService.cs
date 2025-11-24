using REBL;
using REBL.Commands;
using REBL.Tests;

namespace Realizer.Services.Signals;

/// <summary>
/// I want to make a Builder class for building these REBL commands.
/// The idea being similar to StringBuilder in essence, but to build the commands for the REBL console.
/// </summary>
public class PhraseBuilder
{
    private REBLConsole _console;
    public PhraseBuilder(REBLConsole console) => _console = console;

    public Command Claim(string template, string[] expressions)
    {
        var buffer = MakeBuffer($"buffer.{template}");
        var addTemplate = AddTemplate(template);
        var addExpressions = expressions.Select(expression => AddExpression(expression, $"buffer.{template}"));
        var claim = MakeClaim($"buffer.{template}");
        return claim;
    }

    // MakeBuffer private
    private Command MakeBuffer(string bufferName)
    {
        return TesterHelper.MakeBuffer(ref _console, bufferName);
    }

    // AddTemplate private
    private Command AddTemplate(string templateName)
    {
        return TesterHelper.AddTemplate(ref _console, $"buffer.{templateName}", templateName);
    }

    // AddExpression private
    private Command AddExpression(string expressionName, string bufferName)
    {
        return TesterHelper.AddExpression(ref _console, bufferName, expressionName);
    }

    // MakeClaim private
    private Command MakeClaim(string bufferName)
    {
        return TesterHelper.MakeCLaim(ref _console, bufferName);
    }
}

public class RebelService
{
    private REBLConsole _console;
    public RebelService()
    {
        _console = new REBLConsole();
    }

    public string RunHeadless(string command) => _console.RunHeadless(null, command).Result;

    public void NewThread()
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
