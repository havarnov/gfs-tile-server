using Microsoft.Extensions.Caching.Memory;
using NGrib;
using NodaTime;
using NodaTime.Text;

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

public enum ForecastInterval
{
    _00 = 1,
    _06 = 2,
    _12 = 3,
    _18 = 4,
}

public static class ForecastIntervalExtensions
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

public class GFSDataProvider(
    HttpClient client,
    IMemoryCache memoryCache,
    GFSLatestForecastCycleProvider forecastCycleProvider)
{
    private static readonly LocalDatePattern Pattern = LocalDatePattern.CreateWithInvariantCulture("yyyyMMdd");

    public async Task<Wind> GetWind(
        Instant instant,
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        var latest = await forecastCycleProvider.GetLatestForecastCycle(cancellationToken);
        var date = Pattern.Format(latest.Date);
        var interval = latest.Interval.Format();

        var offset = (int)(instant - latest.ToInstant()).TotalHours;
        if (offset < 0)
        {
            throw new ArgumentException($"{nameof(instant)} ({instant:O}) can't be _before_ the latest earliest forecast cycle ({latest.ToInstant():O})", nameof(instant));
        }

        // TODO: verify offset (less than 180 and at specific 3 hour after X hours).

        var url = $"https://nomads.ncep.noaa.gov/cgi-bin/filter_gfs_0p25.pl?dir=%2Fgfs.{date}%2F{interval}%2Fatmos&file=gfs.t{interval}z.pgrb2.0p25.f{offset:000}&var_UGRD=on&var_VGRD=on&lev_10_m_above_ground=on";

        var data = await memoryCache.GetOrCreateAsync(
            url,
            async entry =>
            {
                await using var stream = await client.GetStreamAsync(
                    new Uri(url),
                    cancellationToken);
                await using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, cancellationToken);

                using var reader = new NGrib.Grib2Reader(memoryStream);
                var dataSets = reader.ReadAllDataSets().ToList();
                Wind[] wind = reader.ReadDataSetValues(dataSets[0])
                    .Zip(reader.ReadDataSetValues(dataSets[1]))
                    .Select(i => new Wind
                    {
                        CoordinateA = i.First.Key,
                        CoordinateB = i.Second.Key,
                        U = i.First.Value ?? 0,
                        V = i.Second.Value ?? 0,
                    })
                    .ToArray();

                // expire it one hour after the forecast is "past".
                entry.AbsoluteExpiration = latest.ToInstant()
                    .Plus(Duration.FromHours(offset + 1))
                    .ToDateTimeOffset();

                return new GFSForecastData
                {
                    Wind = wind,
                };
            });

        if (data is null)
        {
            throw new InvalidOperationException("Cache returned null.");
        }

        var (px, py) = LatLonToPixel(latitude, longitude);
        var pIdx = py * 1440 + px;

        return data.Wind[pIdx];
    }

    private static (int x, int y) LatLonToPixel(double latitude, double longitude, int imageWidth = 1440, int imageHeight = 721)
    {
        // Latitude mapping
        int y = (int)((90 - latitude) * (imageHeight / 180.0));

        // Longitude mapping
        int x = 720 + (int)((longitude + 180) * (imageWidth / 360.0));

        return (x, y);
    }
}

public struct Wind
{
    public required Coordinate CoordinateA { get; init; }
    public required Coordinate CoordinateB { get; init; }
    public required float V { get; init; }
    public required float U { get; init; }
}

public class GFSForecastData
{
    public required Wind[] Wind { get; init; }
}