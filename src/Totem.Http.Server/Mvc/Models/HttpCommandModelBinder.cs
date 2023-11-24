namespace Totem.Mvc.Models;

public sealed class HttpCommandModelBinder : IModelBinder
{
    readonly IModelBinder _bodyBinder;

    public HttpCommandModelBinder(IModelBinder bodyBinder) =>
        _bodyBinder = bodyBinder;

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if(!bindingContext.HasMethod(HttpMethod.Post))
        {
            bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"Expected HTTP method {HttpMethod.Post}");
            bindingContext.Result = ModelBindingResult.Failed();
        }
        else
        {
            await _bodyBinder.BindModelAsync(bindingContext);

            if(bindingContext.Result.IsModelSet && bindingContext.Result.Model is not null)
            {
                bindingContext.Model = bindingContext.Result.Model;
            }
        }
    }
}
