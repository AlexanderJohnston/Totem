using Totem.Mvc.Models;

namespace Totem;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromTotemAttribute : Attribute, IBindingSourceMetadata
{
    public BindingSource BindingSource => TotemBindingSource.Totem;
}
