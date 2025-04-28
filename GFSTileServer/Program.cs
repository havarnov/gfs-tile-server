using System.Net;

using GFSTileServer;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using NodaTime;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(static options =>
        options.OutputFormatters.Insert(0, new VectorTileFormatter()));
builder.Services.AddCors(static options =>
    options
        .AddDefaultPolicy(static policyBuilder =>
            policyBuilder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()));

builder.Services.AddSingleton<IClock>(static _ => SystemClock.Instance);
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<GFSLatestForecastCycleProvider>();
builder.Services.AddSingleton<IGFSDataProvider, GFSDataProvider>();

var app = builder.Build();

app.UseExceptionHandler(static exceptionHandlerApp => exceptionHandlerApp
    .Run(static async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new ErrorResponse()
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Message = "Internal server error."
        });
    }));

app.UseRouting();

app.UseCors();

app.MapControllers();

app.Run();