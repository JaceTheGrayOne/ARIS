using Aris.Hosting;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

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
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

try
{
    Log.Information("ARIS backend starting");
    await host.RunAsync();
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
