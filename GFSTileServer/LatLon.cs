namespace GFSTileServer;

/// <summary>
/// Describes a point in a coordinate system.
/// </summary>
public readonly struct LatLon
{
    /// <summary>
    /// The latitude part of this point (y-axis).
    /// </summary>
    public required double Latitude { get; init; }

    /// <summary>
    /// The longitude part of this point (x-axis).
    /// </summary>
    public required double Longitude { get; init; }
}