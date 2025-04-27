using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.VectorTiles;
using NodaTime;
using Tile = NetTopologySuite.IO.VectorTiles.Tiles.Tile;

namespace GFSTileServer;

[ApiController]
public class TileController(
    IClock clock,
    GFSDataProvider dataProvider) : ControllerBase
{
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

        var center = TileCenterToLonLat(x, y, zoom);
        var boundingBox = BoundingBox.From(x, y, zoom);
        var wind = await dataProvider.GetWind(forecastInstant, level, boundingBox, cancellationToken);

        lyr.Features.Add(
            new Feature(
                new Point(center.Longitude, center.Latitude),
                new AttributesTable(new Dictionary<string, object>()
                {
                    { "u", wind.U },
                    { "v", wind.V },
                    { "direction", Math.Atan2(wind.V, wind.U) },
                })));
        vt.Layers.Add(lyr);
        return Ok(vt);
    }

    private static LatLon TileCenterToLonLat(int x, int y, int z)
    {
        var n = Math.Pow(2.0, z);

        // Calculate the longitude of the center
        var lonDeg = ((x + 0.5) / n) * 360.0 - 180.0;

        // Calculate the latitude of the center
        var latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (y + 0.5) / n)));
        var latDeg = latRad * 180.0 / Math.PI;

        return new LatLon()
        {
            Latitude = latDeg,
            Longitude = lonDeg,
        };
    }
}