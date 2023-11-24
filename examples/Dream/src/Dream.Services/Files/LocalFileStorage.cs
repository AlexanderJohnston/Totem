namespace Dream.Files;

public sealed class LocalFileStorage : IFileStorage
{
    readonly string _baseDirectory;

    public LocalFileStorage(string baseDirectory) =>
        _baseDirectory = !string.IsNullOrWhiteSpace(baseDirectory) ? baseDirectory : throw new ArgumentOutOfRangeException(nameof(baseDirectory));

    public Task<bool> ExistsAsync(FilePath path, CancellationToken cancellationToken) =>
        Task.FromResult(File.Exists(GetLocalPath(path)));

    public Task<Stream> GetAsync(FilePath path, CancellationToken cancellationToken)
    {
        var localPath = GetLocalPath(path);

        return Task.FromResult<Stream>(File.OpenRead(localPath));
    }

    public async IAsyncEnumerable<FileItem> ListAsync(IFileQuery query, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var directoryPath = query.Prefix is not null
            ? Path.Combine(_baseDirectory, query.Root, query.Prefix)
            : Path.Combine(_baseDirectory, query.Root);

        foreach(var file in
            from file in new DirectoryInfo(directoryPath).GetFiles("*", SearchOption.AllDirectories)
            let key = GetKey(query.Root, file.FullName)
            where query.IncludeKey(key)
            select new FileItem(new FilePath(query.Root, key), file.Length))
        {
            yield return file;
        }
    }

    public async Task<FileItem> PutAsync(FilePath path, Stream data, CancellationToken cancellationToken)
    {
        var localPath = GetLocalPath(path);
        var directoryPath = Path.GetDirectoryName(localPath)!;

        Directory.CreateDirectory(directoryPath);

        using var file = File.Open(localPath, FileMode.Create);

        await data.CopyToAsync(file, cancellationToken);
        await file.FlushAsync(cancellationToken);

        return new FileItem(path, new FileInfo(localPath).Length);
    }

    public Task RemoveAsync(FilePath path, CancellationToken cancellationToken)
    {
        File.Delete(GetLocalPath(path));

        return Task.CompletedTask;
    }

    string GetLocalPath(FilePath path) =>
        Path.Combine(_baseDirectory, path.Root, NormalizeSeparators(path.Key));

    string GetKey(string root, string fileName) =>
        Path.GetRelativePath(Path.Combine(_baseDirectory, root), fileName);

    static string NormalizeSeparators(string path) =>
        path.Replace("/", "\\").Trim('\\');
}
