namespace Totem.Reports.Bindings;

public interface IHttpReportBinder
{
    IHttpReportBinding<TRow> Bind<TRow>(IHttpReportBindingSource<TRow> source) where TRow : IReportRow;
    IHttpReportListBinding<TRow> BindList<TRow>(IHttpReportListBindingSource<TRow> source) where TRow : IReportRow;
}
