namespace Totem.Mvc.Models;

public sealed class TotemBindingSource : BindingSource
{
    public TotemBindingSource(string id, string displayName, bool isGreedy, bool isFromRequest)
        : base(id, displayName, isGreedy, isFromRequest)
    { }

    public override bool CanAcceptDataFrom(BindingSource bindingSource) =>
        bindingSource == this || bindingSource == Body;

    public static readonly BindingSource Totem = new TotemBindingSource("Totem", "Totem", isGreedy: true, isFromRequest: true);
}
