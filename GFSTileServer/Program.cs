using GFSTileServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(options =>
        options.OutputFormatters.Insert(0, new VectorTileFormatter()));

var app = builder.Build();

app.UseRouting();

app.MapControllers();

app.Run();
