using System.Threading;
using System.Threading.Tasks;

using NodaTime;

namespace GFSTileServer;

internal class GFSLatestForecastCycleProvider(IClock clock)
{
#pragma warning disable IDE0060
    public Task<ForecastCycle> GetLatestForecastCycle(CancellationToken cancellationToken)
#pragma warning restore IDE0060
    {
        var nowUtc = clock.GetCurrentInstant().InUtc();
        var timeOfDayUtc = nowUtc.LocalDateTime.TimeOfDay;

        ForecastCycle forecastCycle;
        if (timeOfDayUtc > new LocalTime(18, 00))
        {
            forecastCycle = new ForecastCycle
            {
                Date = nowUtc.Date,
                Interval = ForecastInterval._12,
            };
        }
        else if (timeOfDayUtc > new LocalTime(12, 00))
        {
            forecastCycle = new ForecastCycle
            {
                Date = nowUtc.Date,
                Interval = ForecastInterval._06,
            };
        }
        else if (timeOfDayUtc > new LocalTime(06, 00))
        {
            forecastCycle = new ForecastCycle
            {
                Date = nowUtc.Date,
                Interval = ForecastInterval._00,
            };
        }
        else
        {
            forecastCycle = new ForecastCycle
            {
                Date = nowUtc.Date.Minus(Period.FromDays(1)),
                Interval = ForecastInterval._18,
            };
        }

        return Task.FromResult(forecastCycle);
    }
}