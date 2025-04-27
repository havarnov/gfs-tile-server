using NodaTime;

namespace GFSTileServer;

public class ForecastCycle
{
    public required LocalDate Date { get; init; }
    public required ForecastInterval Interval { get; init; }

    public Instant ToInstant() => Date
        .AtMidnight()
        .PlusHours(Interval switch
        {
            ForecastInterval._00 => 0,
            ForecastInterval._06 => 6,
            ForecastInterval._12 => 12,
            ForecastInterval._18 => 18,
            _ => throw new ArgumentOutOfRangeException(nameof(Interval), Interval, null)
        })
        .InUtc()
        .ToInstant();
}