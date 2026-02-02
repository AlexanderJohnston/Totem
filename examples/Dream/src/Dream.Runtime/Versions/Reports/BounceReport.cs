using Dream.Test;

namespace Dream.Versions.Reports;

public sealed class BounceReport : Report<BounceRow>
{
    public static Id Route(EchoBounced e) => e.BounceId;
    public static Id Route(DoneBouncing e) => e.BounceId;

    public void When(EchoBounced e) =>
        Row.Test = e.Test;

    public void When(DoneBouncing e) =>
        Row.Done = true;
}
