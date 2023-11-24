namespace Totem.Core;

public sealed class TotemJsonFormat
{
    public TotemJsonFormat(JsonSerializerOptions options) =>
        Options = options;

    public TotemJsonFormat()
    {
        Options = new(JsonSerializerDefaults.Web);

        ConfigureDefaults(Options);
    }

    public JsonSerializerOptions Options { get; }

    public static string ContentType => "application/json";

    public static void ConfigureDefaults(JsonSerializerOptions options)
    {
        options.WriteIndented = true;
        options.Converters.Add(new JsonStringEnumConverter());
    }
}
