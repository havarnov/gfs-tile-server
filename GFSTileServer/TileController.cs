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
    [HttpGet("tiles/gfs/{x}/{y}/{zoom}")]
    public async Task<IActionResult> GetTile(
        [FromRoute] int x,
        [FromRoute] int y,
        [FromRoute] int zoom,
        CancellationToken cancellationToken = default)
    {
        var tileDefinition = new Tile(x, y, zoom);
        var vt = new VectorTile { TileId = tileDefinition.Id, };
        var lyr = new Layer { Name = "testing" };

        var latLon = TileCenterToLonLat(x, y, zoom);

        var wind = await dataProvider.GetWind(clock.GetCurrentInstant(), latLon.Latitude, latLon.Longitude, cancellationToken);

        lyr.Features.Add(
            new Feature(
                new Point(latLon.Longitude, latLon.Latitude),
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

    private class LatLon
    {
        public required double Latitude { get; init; }
        public required double Longitude { get; init; }
    }
}