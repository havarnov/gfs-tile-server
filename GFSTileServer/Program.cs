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

app.UseRouting();

app.MapControllers();

app.Run();
