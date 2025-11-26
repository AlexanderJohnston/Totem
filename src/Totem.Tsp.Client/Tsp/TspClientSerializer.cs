namespace Totem.Tsp;

public sealed class TspClientSerializer : ITspClientSerializer
{
    readonly TotemJsonFormat _jsonFormat;
    readonly Dictionary<string, NotificationType> _notificationsByExternalType;

    public TspClientSerializer(TotemJsonFormat jsonFormat, RuntimeMap map)
    {
        _jsonFormat = jsonFormat;
        _notificationsByExternalType = map.Notifications.AssignableTo<ITspNotification>().ToDictionary(x => x.ExternalType);
    }

    public string SerializeSubscription(ITspSubscription subscription) =>
        JsonSerializer.Serialize(subscription, _jsonFormat.Options);

    public ITspNotification DeserializeNotification(string externalType, string data)
    {
        if(!_notificationsByExternalType.TryGetValue(externalType, out var notification))
            throw new ArgumentException($"Expected external type to map to a TSP notification type: {externalType}", nameof(externalType));

        if(JsonSerializer.Deserialize(data, notification.DeclaredType, _jsonFormat.Options) is not ITspNotification deserialized)
            throw new Exception($"Expected data to deserialize to a TSP subscription of type {notification.DeclaredType}: {data}");

        return deserialized;
    }
}
