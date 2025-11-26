namespace Totem.Tsp;

public interface ITspClientSerializer
{
    string SerializeSubscription(ITspSubscription subscription);
    ITspNotification DeserializeNotification(string externalType, string data);
}
