namespace Totem.InExternal.Services;

public class EventStoreConfig
{
    public string ConnectionString { get; set; } = "esdb://admin:changeit@localhost:2113?tls=false";
}
