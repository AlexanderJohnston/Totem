namespace Totem.Reports.Bindings;

public abstract class ReportBindingBase : IDisposable
{
    const int _unlocked = 0;
    const int _locked = 1;

    readonly CancellationTokenSource _cancellation = new();
    int _reloadLock;
    bool _disposed;

    public Task LoadTask { get; private set; } = null!;

    public Task ReloadAsync(CancellationToken cancellationToken)
    {
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellation.Token).Token;

        if(LoadTask.IsCompleted)
        {
            RunLoadTask(linkedToken);
        }
        else
        {
            WaitToReload(linkedToken);
        }

        return Task.CompletedTask;
    }

    public virtual void NotifyChanged()
    { }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected void RunInitialLoadTask() =>
        RunLoadTask(_cancellation.Token);

    protected abstract Task LoadAsync(CancellationToken cancellationToken);

    protected virtual void Dispose(bool disposing)
    {
        if(!_disposed)
        {
            if(disposing)
            {
                _cancellation.Cancel();
            }

            _disposed = true;
        }
    }

    bool AcquireReloadLock() =>
        Interlocked.CompareExchange(ref _reloadLock, _unlocked, _locked) == _unlocked;

    void ReleaseReloadLock() =>
        Interlocked.Exchange(ref _reloadLock, _unlocked);

    void RunLoadTask(CancellationToken cancellationToken) =>
        LoadTask = Task.Run(() => LoadAsync(cancellationToken), cancellationToken);

    void WaitToReload(CancellationToken cancellationToken)
    {
        if(AcquireReloadLock())
        {
            Task.Run(async () =>
            {
                try
                {
                    await LoadTask.WaitAsync(cancellationToken);

                    RunLoadTask(cancellationToken);
                }
                finally
                {
                    ReleaseReloadLock();
                }
            }, cancellationToken);
        }
    }
}
