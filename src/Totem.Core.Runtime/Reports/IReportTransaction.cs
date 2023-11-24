namespace Totem.Reports;

public interface IReportTransaction
{
    IReportContext<IEvent> Context { get; }
    IReport Report { get; }
    TimelinePosition Position { get; }

    Task CommitAsync();
    Task RollbackAsync();
}
