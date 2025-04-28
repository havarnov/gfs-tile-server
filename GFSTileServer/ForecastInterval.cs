using System;

namespace GFSTileServer;

internal enum ForecastInterval
{
    _00 = 1,
    _06 = 2,
    _12 = 3,
    _18 = 4,
}

internal static class ForecastIntervalExtensions
{
    public static string Format(this ForecastInterval interval)
    {
        return interval switch
        {
            ForecastInterval._00 => "00",
            ForecastInterval._06 => "06",
            ForecastInterval._12 => "12",
            ForecastInterval._18 => "18",
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
        };
    }
}
