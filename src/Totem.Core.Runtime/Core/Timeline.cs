namespace Totem.Core;

public abstract class Timeline : ITimeline, ITimelineInit
{
    readonly ConcurrentQueue<ErrorInfo> _errors = new();

    public Id TimelineId { get; private set; } = null!;
    public bool HasErrors => !_errors.IsEmpty;
    public IEnumerable<ErrorInfo> Errors => _errors;

    Id ITimelineInit.TimelineId { set => TimelineId = value; }

    protected void ThenError(ErrorInfo error) =>
        _errors.Enqueue(error);

    protected void ThenErrors(IEnumerable<ErrorInfo> errors)
    {
        foreach(var error in errors)
        {
            _errors.Enqueue(error);
        }
    }

    protected void ThenErrors(params ErrorInfo[] errors) =>
        ThenErrors(errors.AsEnumerable());
}
