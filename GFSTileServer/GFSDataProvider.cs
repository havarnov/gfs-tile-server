using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;

using NGrib;

using NodaTime;
using NodaTime.Text;

namespace GFSTileServer;

/// <summary>
/// A provider of GFS data.
/// </summary>
public interface IGFSDataProvider
{
    /// <summary>
    /// This method returns GFS wind data for the provided instant, level and bounding box.
    /// </summary>
    /// <param name="instant">The instant to get wind forecast for.</param>
    /// <param name="windLevel">The atmosphere level to get wind forecast for.</param>
    /// <param name="boundingBox">The bounding box to get wind forecast for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel any internal async operations.</param>
    /// <returns>A <see cref="Wind"/> that holds averaged wind data for the specified input.</returns>
    Task<Wind> GetWind(
        Instant instant,
        WindLevel windLevel,
        BoundingBox boundingBox,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets GFS wind data for the provided instant, level and position.
    /// </summary>
    /// <param name="instant">The instant to get wind forecast for.</param>
    /// <param name="windLevel">The atmosphere level to get wind forecast for.</param>
    /// <param name="latLon">The position to get wind forecast for.</param>
    /// <param name="cancellationToken">Cancellation token to cancel any internal async operations.</param>
    /// <returns></returns>
    Task<Wind> GetWind(
        Instant instant,
        WindLevel windLevel,
        LatLon latLon,
        CancellationToken cancellationToken);
}

internal class GFSDataProvider(
    HttpClient client,
    IMemoryCache memoryCache,
    GFSLatestForecastCycleProvider forecastCycleProvider) : IGFSDataProvider
{
    private static readonly LocalDatePattern Pattern = LocalDatePattern.CreateWithInvariantCulture("yyyyMMdd");
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public async Task<Wind> GetWind(
        Instant instant,
        WindLevel windLevel,
        LatLon latLon,
        CancellationToken cancellationToken)
    {
        if (windLevel != WindLevel.M10)
        {
            throw new NotImplementedException("Only M10 levels are supported");
        }

        var latest = await forecastCycleProvider.GetLatestForecastCycle(cancellationToken);
        var date = Pattern.Format(latest.Date);
        var interval = latest.Interval.Format();

        var offset = (int)(instant - latest.ToInstant()).TotalHours;
        if (offset < 0 || (offset > 120 && offset % 3 != 0) || offset > 384)
        {
            throw new ArgumentException($"{nameof(instant)} ({instant:O}) can't be _before_ the latest earliest forecast cycle ({latest.ToInstant():O}) or must match with the forecast offsets (0->120, 123..3..384)", nameof(instant));
        }

        var url = $"https://nomads.ncep.noaa.gov/cgi-bin/filter_gfs_0p25.pl?dir=%2Fgfs.{date}%2F{interval}%2Fatmos&file=gfs.t{interval}z.pgrb2.0p25.f{offset:000}&var_UGRD=on&var_VGRD=on&lev_10_m_above_ground=on";

        // expire it one hour after the forecast is "past".
        var expiration = latest
            .ToInstant()
            .Plus(Duration.FromHours(offset + 1));

        var data = await GetForecastData(url, expiration, cancellationToken);

        var imageWidth = 1440;
        var (pixelX, pixelY) = LatLonToPixel(latLon.Latitude, latLon.Longitude);

        List<double> values = [];

        int[] ys = [.. Range((int)Math.Floor(pixelY), (int)Math.Ceiling(pixelY), 720)];
        int[] xs = [.. Range((int)Math.Floor(pixelX), (int)Math.Ceiling(pixelX), 1440)];

        foreach (var y in ys)
        {
            foreach (var x in xs)
            {
                var index = (y * imageWidth) + x;
                var pixelValue = data.Wind[index];

                values.Add(pixelValue.U);
                values.Add(pixelValue.V);
            }
        }

        alglib.spline2dbuildbicubicv(
            [.. xs.Select(i => (double)i)],
            xs.Length,
            [.. ys.Select(i => (double)i)],
            ys.Length,
            [.. values],
            2,
            out var spline2dInterpolantVector);

        alglib.spline2dcalcv(
            spline2dInterpolantVector,
            pixelX,
            pixelY,
            out var interpolated);

        return new Wind()
        {
            U = (float)interpolated[0],
            V = (float)interpolated[1],
        };
    }

    public async Task<Wind> GetWind(
        Instant instant,
        WindLevel windLevel,
        BoundingBox boundingBox,
        CancellationToken cancellationToken)
    {
        if (windLevel != WindLevel.M10)
        {
            throw new NotImplementedException("Only M10 levels are supported");
        }

        var latest = await forecastCycleProvider.GetLatestForecastCycle(cancellationToken);
        var date = Pattern.Format(latest.Date);
        var interval = latest.Interval.Format();

        var offset = (int)(instant - latest.ToInstant()).TotalHours;
        if (offset < 0 || (offset > 120 && offset % 3 != 0) || offset > 384)
        {
            throw new ArgumentException($"{nameof(instant)} ({instant:O}) can't be _before_ the latest earliest forecast cycle ({latest.ToInstant():O}) or must match with the forecast offsets (0->120, 123..3..384)", nameof(instant));
        }

        var url = $"https://nomads.ncep.noaa.gov/cgi-bin/filter_gfs_0p25.pl?dir=%2Fgfs.{date}%2F{interval}%2Fatmos&file=gfs.t{interval}z.pgrb2.0p25.f{offset:000}&var_UGRD=on&var_VGRD=on&lev_10_m_above_ground=on";

        // expire it one hour after the forecast is "past".
        var expiration = latest
            .ToInstant()
            .Plus(Duration.FromHours(offset + 1));

        var data = await GetForecastData(url, expiration, cancellationToken);

        var imageWidth = 1440;
        var (topLeftX, topLeftY) = LatLonToPixel(boundingBox.NorthWest.Latitude, boundingBox.NorthWest.Longitude);
        var (bottomRightX, bottomRightY) = LatLonToPixel(boundingBox.SouthEast.Latitude, boundingBox.SouthEast.Longitude);

        List<double> u = [];
        List<double> v = [];
        List<double> values = [];

        int[] ys = [.. Range((int)Math.Floor(topLeftY), (int)Math.Ceiling(bottomRightY), 720)];
        int[] xs = [.. Range((int)Math.Floor(topLeftX), (int)Math.Ceiling(bottomRightX), 1440)];

        foreach (var y in ys)
        {
            foreach (var x in xs)
            {
                var index = (y * imageWidth) + x;
                var pixelValue = data.Wind[index];

                u.Add(pixelValue.U);
                v.Add(pixelValue.V);
                values.Add(pixelValue.U);
                values.Add(pixelValue.V);
            }
        }

        if (values.Count <= 8)
        {
            var (pixelX, pixelY) = LatLonToPixel(boundingBox.Center.Latitude, boundingBox.Center.Longitude);
            alglib.spline2dbuildbicubicv(
                [.. xs.Select(i => (double)i)],
                xs.Length,
                [.. ys.Select(i => (double)i)],
                ys.Length,
                [.. values],
                2,
                out var spline2dInterpolantVector);

            alglib.spline2dcalcv(
                spline2dInterpolantVector,
                pixelX,
                pixelY,
                out var interpolated);

            return new Wind() { U = (float)interpolated[0], V = (float)interpolated[1], };
        }

        return new Wind()
        {
            U = (float)u.Average(),
            V = (float)v.Average(),
        };
    }

    private async Task<GFSForecastData> GetForecastData(string url, Instant expiration, CancellationToken cancellationToken)
    {
        var data = memoryCache.Get<GFSForecastData>(url);
        if (data is not null)
        {
            return data;
        }

        var semaphore = Locks.GetOrAdd(url, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            data = memoryCache.Get<GFSForecastData>(url);
            if (data is not null)
            {
                return data;
            }

            await using var stream = await client.GetStreamAsync(
                new Uri(url),
                cancellationToken);
            await using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);

            using var reader = new Grib2Reader(memoryStream);
            var dataSets = reader.ReadAllDataSets().ToList();
            var wind = reader.ReadDataSetValues(dataSets[0])
                .Zip(reader.ReadDataSetValues(dataSets[1]))
                .Select(i => new Wind
                {
                    U = i.First.Value ?? 0,
                    V = i.Second.Value ?? 0,
                })
                .ToArray();

            var absoluteExpiration = expiration.ToDateTimeOffset();

            data = new GFSForecastData
            {
                Wind = wind,
            };

            _ = memoryCache.Set(url, data, absoluteExpiration);
            return data;
        }
        finally
        {
            _ = semaphore.Release();
            if (Locks.TryGetValue(url, out var s) && s.CurrentCount > 0)
            {
                _ = Locks.TryRemove(url, out _);
            }
        }
    }

    private static (double x, double y) LatLonToPixel(double latitude, double longitude, int imageWidth = 1440, int imageHeight = 720)
    {
        // Latitude mapping
        var y = (90 - latitude) * (imageHeight / 180.0);

        // Longitude mapping
        double x;
        if (longitude < 0)
        {
            x = (imageWidth / 2.0) + ((1 - (longitude / -180)) * (imageWidth / 2.0));
        }
        else
        {
            x = longitude * (imageWidth / 360.0);
        }

        return (x, y);
    }

    private static IEnumerable<int> Range(int start, int end, int wrap)
    {
        while (true)
        {
            yield return start;

            start += 1;

            if (start > wrap)
            {
                start = 0;
            }

            if (start == end)
            {
                yield return start;
                break;
            }
        }
    }
}