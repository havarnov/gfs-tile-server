using System;

namespace GFSTileServer;

/// <summary>
/// Describes a "box" in a coordinate system.
/// </summary>
public class BoundingBox
{
    /// <summary>
    /// The upper left (north west) corner of the <see cref="BoundingBox"/>.
    /// </summary>
    public required LatLon NorthWest { get; init; }

    /// <summary>
    /// The bottom right (source east) corner  of the <see cref="BoundingBox"/>.
    /// </summary>
    public required LatLon SouthEast { get; init; }

    /// <summary>
    /// The center point of the <see cref="BoundingBox"/>.
    /// </summary>
    public required LatLon Center { get; init; }

    internal static BoundingBox From(int x, int y, int z)
    {
        var n = Math.Pow(2.0, z);

        // Calculate the longitude bounds
        var lonMin = (x / n * 360.0) - 180.0;
        var lonMax = ((x + 1) / n * 360.0) - 180.0;

        // Calculate the latitude bounds
        var latRadNorth = Math.Atan(Math.Sinh(Math.PI * (1 - (2 * y / n))));
        var latDegNorth = latRadNorth * 180.0 / Math.PI;

        var latRadSouth = Math.Atan(Math.Sinh(Math.PI * (1 - (2 * (y + 1) / n))));
        var latDegSouth = latRadSouth * 180.0 / Math.PI;

        // Calculate the latitude of the center
        var lonDegCenter = ((x + 0.5) / n * 360.0) - 180.0;
        var latRadCenter = Math.Atan(Math.Sinh(Math.PI * (1 - (2 * (y + 0.5) / n))));
        var latDegCenter = latRadCenter * 180.0 / Math.PI;

        return new BoundingBox()
        {
            NorthWest = new LatLon { Latitude = latDegNorth, Longitude = lonMin, },
            SouthEast = new LatLon { Latitude = latDegSouth, Longitude = lonMax, },
            Center = new LatLon { Latitude = latDegCenter, Longitude = lonDegCenter, }
        };
    }
}