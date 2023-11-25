//using EventStore.Client;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using static EventStore.Client.StreamMessage;

//namespace Totem.InExternal.Services
//{
//    public interface IAbstractEventStoreService
//    {
//        Task<bool> StreamExistsAsync(string streamName, CancellationToken cancellationToken);
//        Task<AppendResult> AppendToStreamAsync(string streamName, StreamRevision expectedVersion, IEnumerable<EventData> events, CancellationToken cancellationToken);
//        Task<AppendResult> AppendToStreamAsync(string streamName, StreamRevision expectedVersion, EventData newEvent, CancellationToken cancellationToken);
//        Task<ReadResult> ReadStreamEventsAsync(string streamName, StreamPosition start, int count, CancellationToken cancellationToken);
//        Task<ReadResult> ReadStreamEventsBackwardsAsync(string streamName, int count, CancellationToken cancellationToken);
//        Task TruncateStreamAsync(string streamName, StreamPosition truncatePosition, StreamRevision expectedVersion, CancellationToken cancellationToken);
//        Task DeleteStreamAsync(string streamName, StreamRevision expectedVersion, CancellationToken cancellationToken);
//    }
//}
