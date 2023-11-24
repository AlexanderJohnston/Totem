namespace Totem.UI;

public sealed class ReportQueryUI<TQuery, TRow> : ComponentBase
    where TQuery : IReportQuery<TRow>
    where TRow : IReportRow
{
    [Parameter] public RenderFragment? LoadingInitially { get; set; }
    [Parameter] public RenderFragment? Reloading { get; set; }
    [Parameter] public RenderFragment<TRow>? Loaded { get; set; }
    [Parameter] public RenderFragment<Exception>? LoadError { get; set; }
    [Parameter] public bool AutoReload { get; set; }
    public bool HasChanges { get; private set; }
}
