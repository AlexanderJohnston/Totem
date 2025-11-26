namespace Totem.Core;

public sealed class ObserverRoute
{
    public ObserverRoute(Observation observation, Id observerId)
    {
        Observation = observation;
        ObserverId = observerId;
    }

    public ObserverRoute(Observation observation) : this(observation, Id.NewId())
    { }

    public Observation Observation { get; }
    public ObserverType Observer => Observation.Observer;
    public Id ObserverId { get; }

    public override string ToString() =>
        $"{Observer}.{ObserverId.ToShortString()}";
}
