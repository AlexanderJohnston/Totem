namespace Totem.Reports.Bindings;

public sealed class WebReportBinder : IWebReportBinder, ITspReportWatcher
{
    readonly ConcurrentDictionary<SubscriptionAddress, ReportBindingBase> _bindingsByAddress = new();
    readonly ITotemTspClient _tspClient;

    public WebReportBinder(ITotemTspClient tspClient) =>
        _tspClient = tspClient;

    public IWebReportBinding<TRow> Bind<TRow>(IWebReportBindingSource<TRow> source) where TRow : IReportRow
    {
        var binding = new WebReportBinding<TRow>(this, source, _tspClient);

        _bindingsByAddress[binding.Address] = binding;

        return binding;
    }

    public IWebReportListBinding<TRow> BindList<TRow>(IWebReportListBindingSource<TRow> source) where TRow : IReportRow
    {
        var binding = new WebReportListBinding<TRow>(this, source, _tspClient);

        _bindingsByAddress[binding.Address] = binding;

        return binding;
    }

    public Task NotifyChangedAsync(SubscriptionAddress address, EnvelopeInfo envelopeInfo, CancellationToken cancellationToken)
    {
        if(_bindingsByAddress.TryGetValue(address, out var binding))
        {
            binding.NotifyChanged();
        }

        return Task.CompletedTask;
    }

    internal void RemoveBinding(SubscriptionAddress address) =>
        _bindingsByAddress.Remove(address, out _);
}
