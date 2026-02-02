using IbtTelemetry.Core.Services;
using IbtTelemetry.Service;

var builder = Host.CreateApplicationBuilder(args);

// Register services
builder.Services.AddSingleton<ITelemetryService, TelemetryService>();

// Add Windows Service support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "iRacing Telemetry Service";
});

// Register the worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
