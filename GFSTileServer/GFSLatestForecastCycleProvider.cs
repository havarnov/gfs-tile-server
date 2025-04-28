using System.Threading;
using System.Threading.Tasks;

using NodaTime;

namespace GFSTileServer;

internal class GFSLatestForecastCycleProvider
{
#pragma warning disable IDE0060
    public async Task<ForecastCycle> GetLatestForecastCycle(CancellationToken cancellationToken)
#pragma warning restore IDE0060
    {
        await Task.CompletedTask;
        return new ForecastCycle
        {
            Date = new LocalDate(2025, 04, 26),
            Interval = ForecastInterval._12,
        };
    }
}