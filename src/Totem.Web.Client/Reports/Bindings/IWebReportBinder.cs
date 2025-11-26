namespace Totem.Reports.Bindings;

public interface IWebReportBinder
{
    IWebReportBinding<TRow> Bind<TRow>(IWebReportBindingSource<TRow> source) where TRow : IReportRow;
    IWebReportListBinding<TRow> BindList<TRow>(IWebReportListBindingSource<TRow> source) where TRow : IReportRow;
}
