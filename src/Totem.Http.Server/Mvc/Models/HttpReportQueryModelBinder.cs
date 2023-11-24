namespace Totem.Mvc.Models;

public sealed class HttpReportQueryModelBinder : IModelBinder
{
    readonly RuntimeMap _map;

    public HttpReportQueryModelBinder(RuntimeMap map) =>
        _map = map;

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if(!bindingContext.HasMethod(HttpMethod.Get))
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"Expected HTTP method {HttpMethod.Get}");
            bindingContext.Result = ModelBindingResult.Failed();
        }
        else if(!_map.ReportQueries.TryGet(bindingContext.ModelType, out var queryType))
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Expected mapped report query");
            bindingContext.Result = ModelBindingResult.Failed();
        }
        else if(queryType.Row.Report.IsSingleInstance)
        {
            bindingContext.Result = ModelBindingResult.Success(queryType.CreateSingleInstance());
        }
        else if(!Id.TryFrom(bindingContext.HttpContext.Request.Query["id"], out var id))
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"Expected querystring parameter '{nameof(id)}' to be a GUID");
            bindingContext.Result = ModelBindingResult.Failed();
        }
        else
        {
            bindingContext.Result = ModelBindingResult.Success(queryType.CreateInstance(id));
        }

        return Task.CompletedTask;
    }
}
