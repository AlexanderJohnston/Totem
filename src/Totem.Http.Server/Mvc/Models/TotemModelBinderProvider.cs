namespace Totem.Mvc.Models;

public sealed class TotemModelBinderProvider : IModelBinderProvider
{
    readonly BodyModelBinderProvider _bodyProvider;
    readonly HttpReportQueryModelBinder _reportQueryBinder;
    readonly HttpReportListQueryModelBinder _reportListQueryBinder;

    public TotemModelBinderProvider(BodyModelBinderProvider bodyProvider, RuntimeMap map)
    {
        _bodyProvider = bodyProvider;
        _reportQueryBinder = new(map);
        _reportListQueryBinder = new(map);
    }

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var source = context.BindingInfo.BindingSource;
        var modelType = context.Metadata.ModelType;

        if(source is null || !source.CanAcceptDataFrom(TotemBindingSource.Totem))
        {
            return null;
        }
        else if(typeof(IHttpCommand).IsAssignableFrom(modelType))
        {
            var bodyBinder = _bodyProvider.GetBinder(context);

            return bodyBinder is null ? null : new HttpCommandModelBinder(bodyBinder);
        }
        else if(typeof(IHttpReportQuery).IsAssignableFrom(modelType))
        {
            return _reportQueryBinder;
        }
        else if(typeof(IHttpReportListQuery).IsAssignableFrom(modelType))
        {
            return _reportListQueryBinder;
        }
        else
        {
            return null;
        }
    }
}
