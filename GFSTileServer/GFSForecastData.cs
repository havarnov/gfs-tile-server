namespace GFSTileServer;

internal class GFSForecastData
{
    public required Wind[] Wind { get; init; }
    public required WindPngData WindPngData { get; init; }
}