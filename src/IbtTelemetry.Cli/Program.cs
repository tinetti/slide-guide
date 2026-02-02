using System.CommandLine;
using IbtTelemetry.Cli.Commands;
using IbtTelemetry.Cli.Output;
using IbtTelemetry.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IbtTelemetry.Cli;

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        // Create the root command
        var rootCommand = new RootCommand("iRacing Telemetry Parser - Read and analyze .ibt telemetry files");

        // Create the read command
        var readCommand = CreateReadCommand();
        rootCommand.AddCommand(readCommand);

        // Parse and execute
        return await rootCommand.InvokeAsync(args);
    }

    private static Command CreateReadCommand()
    {
        var readCommand = new Command("read", "Read and display telemetry file information");

        // Define arguments and options
        var fileArgument = new Argument<FileInfo>(
            "file",
            "Path to the .ibt telemetry file")
        {
            Arity = ArgumentArity.ExactlyOne
        };

        var samplesOption = new Option<bool>(
            aliases: new[] { "--samples", "-s" },
            description: "Display telemetry samples");

        var limitOption = new Option<int?>(
            aliases: new[] { "--limit", "-l" },
            description: "Limit the number of samples to display");

        var jsonOption = new Option<bool>(
            aliases: new[] { "--json" },
            description: "Output in JSON format");

        readCommand.AddArgument(fileArgument);
        readCommand.AddOption(samplesOption);
        readCommand.AddOption(limitOption);
        readCommand.AddOption(jsonOption);

        // Set handler
        readCommand.SetHandler(async (FileInfo file, bool samples, int? limit, bool json) =>
        {
            // Build host with DI
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register services
                    services.AddSingleton<ITelemetryService, TelemetryService>();

                    // Register output formatters
                    services.AddSingleton<IOutputFormatter, ConsoleOutputFormatter>();
                    services.AddSingleton<IOutputFormatter, JsonOutputFormatter>();

                    // Register command
                    services.AddSingleton<ReadCommand>();

                    // Configure logging
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(json ? LogLevel.Warning : LogLevel.Information);
                        if (!json)
                        {
                            builder.AddSimpleConsole(options =>
                            {
                                options.IncludeScopes = false;
                                options.TimestampFormat = "HH:mm:ss ";
                            });
                        }
                    });
                })
                .Build();

            // Execute command
            var telemetryService = host.Services.GetRequiredService<ITelemetryService>();
            var formatters = host.Services.GetServices<IOutputFormatter>().ToArray();
            var logger = host.Services.GetRequiredService<ILogger<ReadCommand>>();

            var readCommandInstance = new ReadCommand(telemetryService, formatters, logger);

            Environment.ExitCode = await readCommandInstance.ExecuteAsync(
                file.FullName,
                samples,
                limit,
                json
            );
        },
        fileArgument,
        samplesOption,
        limitOption,
        jsonOption);

        return readCommand;
    }
}
