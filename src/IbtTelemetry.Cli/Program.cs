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
        // Create the root command - primary function is converting .ibt to Parquet
        var rootCommand = new RootCommand("iRacing Telemetry Converter - Convert .ibt files to Parquet for ML");

        // Add default arguments for convert operation
        var inputArgument = new Argument<string>(
            "input",
            "Path to .ibt file or directory containing .ibt files");

        var outputArgument = new Argument<string>(
            "output",
            "Output Parquet file path");

        var allVariablesOption = new Option<bool>(
            aliases: new[] { "--all", "-a" },
            description: "Export all variables (not just default ML variables)");

        var variablesOption = new Option<string[]>(
            aliases: new[] { "--variables", "-v" },
            description: "Comma-separated list of specific variables to export");

        rootCommand.AddArgument(inputArgument);
        rootCommand.AddArgument(outputArgument);
        rootCommand.AddOption(allVariablesOption);
        rootCommand.AddOption(variablesOption);

        // Set default handler (convert/export)
        rootCommand.SetHandler(async (string input, string output, bool all, string[] vars) =>
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ITelemetryService, TelemetryService>();
                    services.AddSingleton<ExportCommand>();
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Information);
                        builder.AddSimpleConsole(options =>
                        {
                            options.IncludeScopes = false;
                            options.TimestampFormat = "HH:mm:ss ";
                        });
                    });
                })
                .Build();

            var exportCommandInstance = host.Services.GetRequiredService<ExportCommand>();

            Environment.ExitCode = await exportCommandInstance.ExecuteAsync(
                input,
                output,
                all,
                vars?.Length > 0 ? vars : null
            );
        },
        inputArgument,
        outputArgument,
        allVariablesOption,
        variablesOption);

        // Add optional subcommands for other functionality
        var readCommand = CreateReadCommand();
        var listVarsCommand = CreateListVariablesCommand();

        rootCommand.AddCommand(readCommand);
        rootCommand.AddCommand(listVarsCommand);

        // Parse and execute
        return await rootCommand.InvokeAsync(args);
    }

    private static Command CreateReadCommand()
    {
        var readCommand = new Command("read", "Inspect and display telemetry file contents (diagnostic)");

        // Define arguments and options
        var pathArgument = new Argument<string>(
            "path",
            "Path to .ibt file or directory containing .ibt files")
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

        readCommand.AddArgument(pathArgument);
        readCommand.AddOption(samplesOption);
        readCommand.AddOption(limitOption);
        readCommand.AddOption(jsonOption);

        // Set handler
        readCommand.SetHandler(async (string path, bool samples, int? limit, bool json) =>
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
                path,
                samples,
                limit,
                json
            );
        },
        pathArgument,
        samplesOption,
        limitOption,
        jsonOption);

        return readCommand;
    }


    private static Command CreateListVariablesCommand()
    {
        var listVarsCommand = new Command("list-vars", "List all available telemetry variables");

        var fileArgument = new Argument<FileInfo>(
            "file",
            "Path to the .ibt telemetry file");

        listVarsCommand.AddArgument(fileArgument);

        listVarsCommand.SetHandler(async (FileInfo file) =>
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ITelemetryService, TelemetryService>();
                    services.AddSingleton<ExportCommand>();
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Warning);
                    });
                })
                .Build();

            var exportCommandInstance = host.Services.GetRequiredService<ExportCommand>();

            Environment.ExitCode = await exportCommandInstance.ListVariablesAsync(file.FullName);
        },
        fileArgument);

        return listVarsCommand;
    }
}
