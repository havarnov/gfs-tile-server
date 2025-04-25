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

        var latLon = TileToLonLat(x, y, zoom);

        var wind = await dataProvider.Foo(clock.GetCurrentInstant(), latLon.Latitude, latLon.Longitude, cancellationToken);

        Console.WriteLine("CONTROLLER:" + latLon.Latitude + " " + latLon.Longitude + " " + wind.CoordinateA + " " + wind.CoordinateB + " " + wind.U + "|" + wind.V);
        lyr.Features.Add(
            new Feature(
                new Point(latLon.Longitude, latLon.Latitude),
                new AttributesTable(new Dictionary<string, object>()
                {
                    { "u", wind.V },
                    { "v", wind.U },
                    { "direction", Math.Atan2(wind.U, wind.V) },
                })));
        vt.Layers.Add(lyr);
        return Ok(vt);
    }
    private static LatLon TileToLonLat(int x, int y, int z)
    {
        double n = Math.Pow(2.0, z);
        double lon_deg = ((x / n) * 360.0) - 180.0;
        double lat_rad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / n)));
        double lat_deg = lat_rad * 180.0 / Math.PI;
        return new LatLon()
        {
            Latitude = lat_deg,
            Longitude = lon_deg,
        };
    }

    private class LatLon
    {
        public required double Latitude { get; init; }
        public required double Longitude { get; init; }
    }
}