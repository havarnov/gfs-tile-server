namespace GFSTileServer;

public class BoundingBox
{
    public required LatLon NorthWest { get; init; }
    public required LatLon SouthEast { get; init; }

    public static BoundingBox From(int x, int y, int z)
    {
        var n = Math.Pow(2.0, z);

        // Calculate the longitude bounds
        var lonMin = (x / n) * 360.0 - 180.0;
        var lonMax = ((x + 1) / n) * 360.0 - 180.0;

        // Calculate the latitude bounds
        var latRadNorth = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / n)));
        var latDegNorth = latRadNorth * 180.0 / Math.PI;

        var latRadSouth = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (y + 1) / n)));
        var latDegSouth = latRadSouth * 180.0 / Math.PI;

        return new BoundingBox()
        {
            NorthWest = new LatLon { Latitude = latDegNorth, Longitude = lonMin },
            SouthEast = new LatLon { Latitude = latDegSouth, Longitude = lonMax },
        };
    }
}