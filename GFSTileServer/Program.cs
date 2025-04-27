using System.Net;
using GFSTileServer;
using NodaTime;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(options =>
        options.OutputFormatters.Insert(0, new VectorTileFormatter()));


builder.Services.AddSingleton<IClock>(_ => SystemClock.Instance);
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<GFSLatestForecastCycleProvider>();
builder.Services.AddSingleton<GFSDataProvider>();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new ErrorResponse()
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Message = "Internal server error."
        });
    });
});

app.UseRouting();

app.MapControllers();

app.Run();