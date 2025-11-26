namespace Totem.Reports.Bindings;

public sealed class ReportBinder : IReportBinder, IReportWatcher
{
    readonly ConcurrentDictionary<SubscriptionAddress, ReportBindingBase> _bindingsByAddress = new();
    readonly IReportQueryPipeline _queryPipeline;
    readonly IReportListQueryPipeline _listQueryPipeline;

    public ReportBinder(IReportQueryPipeline queryPipeline, IReportListQueryPipeline listQueryPipeline)
    {
        _queryPipeline = queryPipeline;
        _listQueryPipeline = listQueryPipeline;
    }

    public IReportBinding<TRow> Bind<TRow>(IReportBindingSource<TRow> source) where TRow : IReportRow
    {
        var binding = new ReportBinding<TRow>(this, source, _queryPipeline);

        _bindingsByAddress[binding.Address] = binding;

        return binding;
    }

    public IReportListBinding<TRow> BindList<TRow>(IReportListBindingSource<TRow> source) where TRow : IReportRow
    {
        var binding = new ReportListBinding<TRow>(this, source, _listQueryPipeline);

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
