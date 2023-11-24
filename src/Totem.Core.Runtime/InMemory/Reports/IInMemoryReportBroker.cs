namespace Totem.InMemory.Reports;

public interface IInMemoryReportBroker
{
    void PublishChanged(ReportType report, TimelineVersion newVersion);
}
