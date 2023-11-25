namespace Realizer.Files;

public sealed class FileQuery : IFileQuery
{
    readonly Func<string, bool>? _includeKey;

    FileQuery(string root) =>
        Root = !string.IsNullOrWhiteSpace(root) ? root : throw new ArgumentOutOfRangeException(nameof(root));

    FileQuery(string root, string prefix) : this(root) =>
        Prefix = !string.IsNullOrWhiteSpace(prefix) ? prefix : throw new ArgumentOutOfRangeException(nameof(prefix));

    FileQuery(string root, Func<string, bool> includeKey) : this(root) =>
        _includeKey = includeKey;

    FileQuery(string root, string prefix, Func<string, bool> includeKey) : this(root, prefix) =>
        _includeKey = includeKey;

    public string Root { get; }
    public string? Prefix { get; }

    public bool IncludeKey(string key)
    {
        if(string.IsNullOrWhiteSpace(key))
            throw new ArgumentOutOfRangeException(nameof(key));

        return _includeKey?.Invoke(key) ?? true;
    }

    public static IFileQuery FromRoot(string root) =>
        new FileQuery(root);

    public static IFileQuery FromRoot(string root, Func<string, bool> includeKey) =>
        new FileQuery(root, includeKey);

    public static IFileQuery FromRoot(string root, IEnumerable<string> extensions) =>
        new FileQuery(root, IncludeExtensions(extensions));

    public static IFileQuery FromRoot(string root, params string[] extensions) =>
        new FileQuery(root, IncludeExtensions(extensions));

    public static IFileQuery FromRoot(string root, Func<string, bool> includeKey, IEnumerable<string> extensions) =>
        new FileQuery(root, IncludeKeys(includeKey, extensions));

    public static IFileQuery FromRoot(string root, Func<string, bool> includeKey, params string[] extensions) =>
        new FileQuery(root, IncludeKeys(includeKey, extensions));

    public static IFileQuery From(string root, string prefix) =>
        new FileQuery(root, prefix);

    public static IFileQuery From(string root, string prefix, Func<string, bool> includeKey) =>
        new FileQuery(root, prefix, includeKey);

    public static IFileQuery From(string root, string prefix, IEnumerable<string> extensions) =>
        new FileQuery(root, prefix, IncludeExtensions(extensions));

    public static IFileQuery From(string root, string prefix, params string[] extensions) =>
        new FileQuery(root, prefix, IncludeExtensions(extensions));

    public static IFileQuery From(string root, string prefix, Func<string, bool> includeKey, IEnumerable<string> extensions) =>
        new FileQuery(root, prefix, IncludeKeys(includeKey, extensions));

    public static IFileQuery From(string root, string prefix, Func<string, bool> includeKey, params string[] extensions) =>
        new FileQuery(root, prefix, IncludeKeys(includeKey, extensions));

    static Func<string, bool> IncludeExtensions(IEnumerable<string> extensions) =>
        key => extensions.Contains(Path.GetExtension(key));

    static Func<string, bool> IncludeKeys(Func<string, bool> includeKey, IEnumerable<string> extensions) =>
        key => includeKey(key) && extensions.Contains(Path.GetExtension(key));
}
