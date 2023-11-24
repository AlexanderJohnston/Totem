namespace Totem.Mvc.Conventions;

public sealed class InternalPrefixConvention : IApplicationModelConvention
{
    readonly string _prefix;

    public InternalPrefixConvention(string prefix) =>
        _prefix = !string.IsNullOrWhiteSpace(prefix) ? prefix : throw new ArgumentOutOfRangeException(nameof(prefix));

    public void Apply(ApplicationModel application)
    {
        var thisAssembly = Assembly.GetExecutingAssembly();
        
        var internalSelectors =
            from controller in application.Controllers
            where controller.ControllerType.Assembly == thisAssembly
            let selector = controller.Selectors.Single()
            let template = selector.AttributeRouteModel?.Template
            where template is not null && !template.StartsWith(_prefix)
            select selector;

        var prefixModel = new AttributeRouteModel(new RouteAttribute(_prefix));

        foreach(var selector in internalSelectors)
        {
            selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(prefixModel, selector.AttributeRouteModel);
        }
    }
}
