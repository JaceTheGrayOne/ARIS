using Aris.Hosting;
using Aris.Hosting.Endpoints;
using Aris.Hosting.Infrastructure;
using Aris.Infrastructure.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.Configure<WorkspaceOptions>(
    builder.Configuration.GetSection("Workspace"));

builder.Services.AddSingleton<BackendHealthState>();
builder.Services.AddHostedService<ToolingStartupHostedService>();

var app = builder.Build();

app.MapHealthAndInfoEndpoints();

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
