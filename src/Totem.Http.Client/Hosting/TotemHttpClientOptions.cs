namespace Totem.Hosting;

public sealed class TotemHttpClientOptions
{
    public Uri? BaseAddress { get; set; }
    public string RoutePrefix { get; set; } = "_totem";
}
