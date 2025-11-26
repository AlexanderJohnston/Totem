namespace Totem.Reports.Bindings;

public interface IReportBinder
{
    IReportBinding<TRow> Bind<TRow>(IReportBindingSource<TRow> source) where TRow : IReportRow;
    IReportListBinding<TRow> BindList<TRow>(IReportListBindingSource<TRow> source) where TRow : IReportRow;
}
