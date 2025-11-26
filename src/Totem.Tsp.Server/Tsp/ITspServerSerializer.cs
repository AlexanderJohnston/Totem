namespace Totem.Tsp;

public interface ITspServerSerializer
{
    ITspSubscription DeserializeSubscription(string externalType, string data);
    string SerializeNotification(ITspNotification notification);
}
