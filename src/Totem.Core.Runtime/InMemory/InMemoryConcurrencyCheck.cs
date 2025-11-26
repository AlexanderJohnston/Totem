namespace Totem.InMemory;

internal static class InMemoryConcurrencyCheck
{
    internal static void CheckConcurrency(this TimelineKey timelineKey, TimelinePosition streamPosition, TimelinePosition transactionPosition)
    {
        if(streamPosition.IsStart && !transactionPosition.IsStart)
            throw new TimelineConcurrencyException($"Expected {timelineKey} @ stream start but received v{transactionPosition}");

        if(!streamPosition.IsStart && transactionPosition.IsStart)
            throw new TimelineConcurrencyException($"Expected {timelineKey} @ v{streamPosition} but received stream start");

        if(streamPosition != transactionPosition)
            throw new TimelineConcurrencyException($"Expected {timelineKey} @ v{streamPosition} but received v{transactionPosition}");
    }
}
