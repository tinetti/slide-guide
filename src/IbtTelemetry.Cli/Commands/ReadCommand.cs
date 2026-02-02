using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IbtTelemetry.Cli.Output;
using IbtTelemetry.Core.Services;
using Microsoft.Extensions.Logging;

namespace IbtTelemetry.Cli.Commands;

/// <summary>
/// Command to read and display telemetry file information
/// </summary>
public class ReadCommand
{
    private readonly ITelemetryService _telemetryService;
    private readonly IOutputFormatter[] _formatters;
    private readonly ILogger<ReadCommand> _logger;

    public ReadCommand(
        ITelemetryService telemetryService,
        IOutputFormatter[] formatters,
        ILogger<ReadCommand> logger)
    {
        _telemetryService = telemetryService;
        _formatters = formatters;
        _logger = logger;
    }

    /// <summary>
    /// Execute the read command
    /// </summary>
    public async Task<int> ExecuteAsync(
        string filePath,
        bool showSamples,
        int? limit,
        bool jsonOutput,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading telemetry file: {FilePath}", filePath);

            // Load telemetry file
            using var telemetry = await _telemetryService.LoadFromFileAsync(filePath, cancellationToken);

            _logger.LogInformation("Loaded {NumVars} variables and {NumBuf} samples",
                telemetry.Header.NumVars, telemetry.Header.NumBuf);

            // Select formatter
            var formatter = SelectFormatter(jsonOutput);

            // Display session info
            formatter.DisplaySessionInfo(telemetry);

            // Display samples if requested
            if (showSamples)
            {
                await DisplaySamplesAsync(telemetry, formatter, limit, cancellationToken);
            }
            else if (!jsonOutput)
            {
                // Close the JSON object if not showing samples
                if (jsonOutput)
                {
                    Console.WriteLine("}");
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading telemetry file");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private IOutputFormatter SelectFormatter(bool jsonOutput)
    {
        var formatName = jsonOutput ? "json" : "console";
        var formatter = _formatters.FirstOrDefault(f => f.FormatName == formatName);

        if (formatter == null)
        {
            throw new InvalidOperationException($"Formatter '{formatName}' not found");
        }

        return formatter;
    }

    private async Task DisplaySamplesAsync(
        Core.Models.Telemetry telemetry,
        IOutputFormatter formatter,
        int? limit,
        CancellationToken cancellationToken)
    {
        formatter.DisplaySampleHeader();

        var sampleCount = limit ?? telemetry.Header.NumBuf;
        var samplesDisplayed = 0;

        await foreach (var sample in telemetry.GetSamplesAsync(cancellationToken))
        {
            formatter.DisplaySample(sample, samplesDisplayed + 1);

            samplesDisplayed++;
            if (samplesDisplayed >= sampleCount)
            {
                break;
            }
        }

        formatter.DisplaySampleFooter();

        _logger.LogInformation("Displayed {Count} samples", samplesDisplayed);
    }
}
