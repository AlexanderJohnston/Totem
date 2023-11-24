namespace Totem.Logging;

public sealed class RuntimeEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var updates = null as List<LogEventProperty>;

        foreach(var property in logEvent.Properties)
        {
            if(TryShorten(property, out var shortened))
            {
                updates ??= new List<LogEventProperty>();
                updates.Add(new LogEventProperty(property.Key, new ScalarValue(shortened)));
            }
        }

        if(updates is not null)
        {
            foreach(var update in updates)
            {
                logEvent.AddOrUpdateProperty(update);
            }
        }
    }

    static bool TryShorten(KeyValuePair<string, LogEventPropertyValue> property, [NotNullWhen(true)] out string? shortened)
    {
        shortened = property.Value switch
        {
            ScalarValue { Value: Id id } => id.ToShortString(),
            ScalarValue { Value: Type type } when CanShorten(type) => type.Name,
            ScalarValue { Value: RuntimeType runtimeType } when CanShorten(runtimeType.DeclaredType) => runtimeType.DeclaredType.Name,
            _ => null
        };

        return shortened is not null;
    }

    static bool CanShorten(Type type) =>
        typeof(IMessage).IsAssignableFrom(type) || typeof(ITimeline).IsAssignableFrom(type);
}
