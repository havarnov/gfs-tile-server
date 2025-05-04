using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.VectorTiles;

using NodaTime;

using Tile = NetTopologySuite.IO.VectorTiles.Tiles.Tile;

namespace GFSTileServer;

/// <summary>
/// Creates tiles dynamically from GFS model data.
/// </summary>
[ApiController]
public class TileController(
    IClock clock,
    IGFSDataProvider dataProvider)
    : ControllerBase
{
    /// <summary>
    /// Get vector tiles with wind data.
    /// </summary>
    /// <param name="forecastInstant"></param>
    /// <param name="level"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="zoom"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [HttpGet("tiles/gfs/{forecastInstant}/wind/{level}/{x}/{y}/{zoom}")]
    public async Task<IActionResult> GetTile(
        [FromRoute] Instant forecastInstant,
        [FromRoute] WindLevel level,
        [FromRoute] int x,
        [FromRoute] int y,
        [FromRoute] int zoom,
        CancellationToken cancellationToken = default)
    {

        var nowUtc = clock.GetCurrentInstant().InUtc();
        var startOfCurrentHourUtc = new LocalDateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, 0, 0);
        var forecastUtc = forecastInstant.InUtc();
        if (forecastUtc.LocalDateTime < startOfCurrentHourUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(forecastInstant), forecastInstant, "forecastInstant must be after the start of the current hour.");
        }

        var tileDefinition = new Tile(x, y, zoom);
        var vt = new VectorTile { TileId = tileDefinition.Id, };
        var lyr = new Layer { Name = "testing" };

        var boundingBox = BoundingBox.From(x, y, zoom);
        var wind = await dataProvider.GetWind(forecastInstant, level, boundingBox, cancellationToken);

        lyr.Features.Add(
            new Feature(
                new Point(boundingBox.Center.Longitude, boundingBox.Center.Latitude),
                new AttributesTable(new Dictionary<string, object>()
                {
                    { "u", wind.U },
                    { "v", wind.V },
                    { "direction", Math.Atan2(wind.V, wind.U) },
                })));
        vt.Layers.Add(lyr);
        return Ok(vt);
    }

    /// <summary>
    /// Get wind data for the specified position.
    /// </summary>
    /// <param name="forecastInstant"></param>
    /// <param name="level"></param>
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [HttpGet("position/gfs/{forecastInstant}/wind/{level}/{latitude}/{longitude}")]
    public async Task<IActionResult> GetPosition(
        [FromRoute] Instant forecastInstant,
        [FromRoute] WindLevel level,
        [FromRoute] double latitude,
        [FromRoute] double longitude,
        CancellationToken cancellationToken = default)
    {

        var nowUtc = clock.GetCurrentInstant().InUtc();
        var startOfCurrentHourUtc = new LocalDateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, 0, 0);
        var forecastUtc = forecastInstant.InUtc();
        if (forecastUtc.LocalDateTime < startOfCurrentHourUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(forecastInstant), forecastInstant, "forecastInstant must be after the start of the current hour.");
        }

        var wind = await dataProvider.GetWind(forecastInstant, level, new LatLon() { Latitude = latitude, Longitude = longitude, }, cancellationToken);

        return Ok(wind);
    }
}
