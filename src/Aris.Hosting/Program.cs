using Aris.Hosting;
using Aris.Hosting.Endpoints;
using Aris.Hosting.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure CORS for frontend dev server - ONLY in Development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy
                .WithOrigins("http://localhost:5173")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
}

var logsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "ARIS",
    "logs",
    "aris-.log");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        logsPath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

builder.Services.AddSerilog();
builder.Services.AddArisBackend(builder.Configuration);

builder.Services.AddSingleton<BackendHealthState>();
builder.Services.AddHostedService<ToolingStartupHostedService>();

// Register URL announcement service - announces bound URL to stdout for UI discovery
builder.Services.AddHostedService<UrlAnnouncementService>();

var app = builder.Build();

// Enable WebSocket support
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// Enable CORS only in Development (production uses same-origin via static file serving)
if (app.Environment.IsDevelopment())
{
    app.UseCors();
}

// Serve frontend static files from wwwroot (production)
app.UseStaticFiles();

// Map API and health endpoints
app.MapHealthAndInfoEndpoints();
app.MapRetocEndpoints();
app.MapUAssetEndpoints();
app.MapUwpDumperEndpoints();
app.MapDllInjectorEndpoints();
app.MapToolDocsEndpoints();

// SPA fallback - serve index.html for unmatched routes (must be after API endpoints)
app.MapFallbackToFile("index.html");

try
{
    Log.Information("ARIS backend starting");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ARIS backend terminated unexpectedly");
}
finally
{
    Log.Information("ARIS backend shutdown complete");
    await Log.CloseAndFlushAsync();
}
