namespace Totem.External;
public class ExternalStorage : IStorage
{
    public Task<IStorageRow<T>> CreateAsync<T>(IStorageRow<T> row, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IStorageRow?> GetAsync(StorageKey key, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IStorageRow<T>?> GetAsync<T>(StorageKey key, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IReadOnlyList<IStorageRow>> ListAsync(string partitionKey, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IReadOnlyList<IStorageRow<T>>> ListAsync<T>(string partitionKey, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IStorageRow<T>> PutAsync<T>(IStorageRow<T> row, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<IStorageRow> PutAsync(IStorageRow row, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task PutAsync<T>(IEnumerable<IStorageRow<T>> rows, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task PutAsync(IEnumerable<IStorageRow> rows, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task RemoveAsync(StorageKey key, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task RemoveAsync(IEnumerable<StorageKey> keys, CancellationToken cancellationToken) => throw new NotImplementedException();
}
