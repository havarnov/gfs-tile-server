namespace GFSTileServer;

/// <summary>
/// A wind vector.
/// </summary>
public readonly struct Wind
{
    /// <summary>
    /// The V component of this wind vector (y-axis).
    /// </summary>
    public required float V { get; init; }

    /// <summary>
    /// The U component of this wind vector (x-axis).
    /// </summary>
    public required float U { get; init; }
}