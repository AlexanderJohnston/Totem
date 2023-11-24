namespace Totem.Tsp;

public sealed class TspServerSerializer : ITspServerSerializer
{
    readonly TotemJsonFormat _jsonFormat;
    readonly Dictionary<string, SubscriptionType> _subscriptionsByExternalType;

    public TspServerSerializer(RuntimeMap map, TotemJsonFormat jsonFormat)
    {
        _jsonFormat = jsonFormat;
        _subscriptionsByExternalType = map.Subscriptions.AssignableTo<ITspSubscription>().ToDictionary(x => x.ExternalType);
    }

    public string SerializeNotification(ITspNotification notification) =>
        JsonSerializer.Serialize(notification, _jsonFormat.Options);

    public ITspSubscription DeserializeSubscription(string externalType, string data)
    {
        if(!_subscriptionsByExternalType.TryGetValue(externalType, out var subscription))
            throw new ArgumentException($"Expected external type to map to a TSP subscription type: {externalType}", nameof(externalType));

        if(JsonSerializer.Deserialize(data, subscription.DeclaredType, _jsonFormat.Options) is not ITspSubscription deserialized)
            throw new Exception($"Expected data to deserialize to a TSP subscription of type {subscription.DeclaredType}: {data}");

        return deserialized;
    }
}
