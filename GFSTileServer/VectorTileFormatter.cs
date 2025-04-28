using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.Formatters;

using NetTopologySuite.IO.VectorTiles;
using NetTopologySuite.IO.VectorTiles.Mapbox;

namespace GFSTileServer;

internal class VectorTileFormatter : OutputFormatter
{
    public VectorTileFormatter()
    {
        SupportedMediaTypes.Add("application/vnd.mapbox-vector-tile");
    }

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        if (context.Object is not VectorTile tile)
        {
            throw new ArgumentException();
        }

        using var memoryStream = new MemoryStream();

        context.HttpContext.Response.ContentType = "application/vnd.mapbox-vector-tile";
        var stream = context.HttpContext.Response.BodyWriter.AsStream();

        tile.Write(
            stream,
            MapboxTileWriter.DefaultMinLinealExtent * 4,
            MapboxTileWriter.DefaultMinPolygonalExtent);

        return Task.CompletedTask;
    }

    protected override bool CanWriteType(Type? type)
    {
        return typeof(VectorTile).IsAssignableFrom(type);
    }
}