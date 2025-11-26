namespace Totem.Mvc.Models;

public sealed class HttpReportListQueryModelBinder : IModelBinder
{
    readonly RuntimeMap _map;

    public HttpReportListQueryModelBinder(RuntimeMap map) =>
        _map = map;

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if(!bindingContext.HasMethod(HttpMethod.Get))
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"Expected HTTP method {HttpMethod.Get}");
            bindingContext.Result = ModelBindingResult.Failed();
        }
        else if(!_map.ReportListQueries.TryGet(bindingContext.ModelType, out var queryType))
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Expected mapped report list query");
            bindingContext.Result = ModelBindingResult.Failed();
        }
        else
        {
            bindingContext.Result = ModelBindingResult.Success(queryType.CreateInstance());
        }

        return Task.CompletedTask;
    }
}
