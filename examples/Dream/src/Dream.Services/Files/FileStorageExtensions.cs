namespace Dream.Files;

public static class FileStorageExtensions
{
    public static Task<bool> ExistsAsync<T>(this IFileStorage storage, string root, string key, CancellationToken cancellationToken) =>
        storage.ExistsAsync(new FilePath(root, key), cancellationToken);

    public static Task<Stream> GetAsync(this IFileStorage storage, string root, string key, CancellationToken cancellationToken) =>
        storage.GetAsync(new FilePath(root, key), cancellationToken);

    public static IAsyncEnumerable<FileItem> ListAsync(this IFileStorage storage, string root, Func<string, bool> includeKey, CancellationToken cancellationToken) =>
        storage.ListAsync(FileQuery.FromRoot(root, includeKey), cancellationToken);

    public static IAsyncEnumerable<FileItem> ListAsync(this IFileStorage storage, string root, IEnumerable<string> extensions, CancellationToken cancellationToken) =>
        storage.ListAsync(FileQuery.FromRoot(root, extensions), cancellationToken);

    public static IAsyncEnumerable<FileItem> ListAsync(this IFileStorage storage, string root, Func<string, bool> includeKey, IEnumerable<string> extensions, CancellationToken cancellationToken) =>
        storage.ListAsync(FileQuery.FromRoot(root, includeKey, extensions), cancellationToken);

    public static IAsyncEnumerable<FileItem> ListAsync(this IFileStorage storage, string root, string prefix, Func<string, bool> includeKey, CancellationToken cancellationToken) =>
        storage.ListAsync(FileQuery.From(root, prefix, includeKey), cancellationToken);

    public static IAsyncEnumerable<FileItem> ListAsync(this IFileStorage storage, string root, string prefix, IEnumerable<string> extensions, CancellationToken cancellationToken) =>
        storage.ListAsync(FileQuery.From(root, prefix, extensions), cancellationToken);

    public static IAsyncEnumerable<FileItem> ListAsync(this IFileStorage storage, string root, string prefix, Func<string, bool> includeKey, IEnumerable<string> extensions, CancellationToken cancellationToken) =>
        storage.ListAsync(FileQuery.From(root, prefix, includeKey, extensions), cancellationToken);

    public static Task<FileItem> PutAsync(this IFileStorage storage, string root, string key, Stream data, CancellationToken cancellationToken) =>
        storage.PutAsync(new FilePath(root, key), data, cancellationToken);

    public static Task RemoveAsync(this IFileStorage storage, string root, string key, CancellationToken cancellationToken) =>
        storage.RemoveAsync(new FilePath(root, key), cancellationToken);
}
