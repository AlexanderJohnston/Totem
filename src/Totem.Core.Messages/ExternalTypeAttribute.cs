namespace Totem;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ExternalTypeAttribute : Attribute
{
    public ExternalTypeAttribute(string externalType) =>
        ExternalType = !string.IsNullOrWhiteSpace(externalType) ? externalType : throw new ArgumentOutOfRangeException(nameof(externalType));

    public string ExternalType { get; }
}
