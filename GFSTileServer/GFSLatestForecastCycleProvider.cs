using NodaTime;

namespace GFSTileServer;

public class GFSLatestForecastCycleProvider
{
    public async Task<ForecastCycle> GetLatestForecastCycle(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new ForecastCycle
        {
            Date =  new LocalDate(2025, 04, 26),
            Interval = ForecastInterval._12,
        };
    }
}