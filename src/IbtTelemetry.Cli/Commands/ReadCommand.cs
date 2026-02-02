using System;
using System.Collections.Generic;
using System.IO;
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
        string path,
        bool showSamples,
        int? limit,
        bool jsonOutput,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if path is a directory or file
            if (Directory.Exists(path))
            {
                return await ProcessDirectoryAsync(path, showSamples, limit, jsonOutput, cancellationToken);
            }
            else if (File.Exists(path))
            {
                return await ProcessSingleFileAsync(path, showSamples, limit, jsonOutput, cancellationToken);
            }
            else
            {
                Console.Error.WriteLine($"Error: Path '{path}' does not exist");
                return 1;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing telemetry");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Process a single telemetry file
    /// </summary>
    private async Task<int> ProcessSingleFileAsync(
        string filePath,
        bool showSamples,
        int? limit,
        bool jsonOutput,
        CancellationToken cancellationToken)
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

            // Always display samples - default to 5 if not specified
            var sampleLimit = showSamples ? limit : (limit ?? 5);
            await DisplaySamplesAsync(telemetry, formatter, sampleLimit, cancellationToken);

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading telemetry file: {FilePath}", filePath);
            Console.Error.WriteLine($"Error processing {Path.GetFileName(filePath)}: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Process all .ibt files in a directory and subdirectories
    /// </summary>
    private async Task<int> ProcessDirectoryAsync(
        string directoryPath,
        bool showSamples,
        int? limit,
        bool jsonOutput,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scanning directory: {DirectoryPath}", directoryPath);

        // Find all .ibt files recursively
        var ibtFiles = Directory.GetFiles(directoryPath, "*.ibt", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();

        if (ibtFiles.Count == 0)
        {
            Console.WriteLine($"No .ibt files found in {directoryPath}");
            return 1;
        }

        if (!jsonOutput)
        {
            Console.WriteLine($"\nFound {ibtFiles.Count} .ibt file(s) in {directoryPath}");
            Console.WriteLine(new string('=', 70));
        }

        var successCount = 0;
        var failureCount = 0;
        var results = new List<FileProcessingResult>();

        foreach (var filePath in ibtFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!jsonOutput)
            {
                Console.WriteLine($"\n[{successCount + failureCount + 1}/{ibtFiles.Count}] Processing: {Path.GetFileName(filePath)}");
                Console.WriteLine(new string('-', 70));
            }

            var result = await ProcessSingleFileAsync(filePath, showSamples, limit, jsonOutput, cancellationToken);

            if (result == 0)
            {
                successCount++;
                results.Add(new FileProcessingResult
                {
                    FilePath = filePath,
                    Success = true
                });
            }
            else
            {
                failureCount++;
                results.Add(new FileProcessingResult
                {
                    FilePath = filePath,
                    Success = false
                });
            }

            if (!jsonOutput && successCount + failureCount < ibtFiles.Count)
            {
                Console.WriteLine("\n" + new string('=', 70));
            }
        }

        // Display summary
        if (!jsonOutput)
        {
            Console.WriteLine("\n" + new string('=', 70));
            Console.WriteLine("PROCESSING SUMMARY");
            Console.WriteLine(new string('=', 70));
            Console.WriteLine($"Total files:    {ibtFiles.Count}");
            Console.WriteLine($"Successful:     {successCount}");
            Console.WriteLine($"Failed:         {failureCount}");

            if (failureCount > 0)
            {
                Console.WriteLine("\nFailed files:");
                foreach (var result in results.Where(r => !r.Success))
                {
                    Console.WriteLine($"  ✗ {Path.GetFileName(result.FilePath)}");
                }
            }
        }

        return failureCount > 0 ? 1 : 0;
    }

    /// <summary>
    /// Result of processing a file
    /// </summary>
    private class FileProcessingResult
    {
        public string FilePath { get; set; } = string.Empty;
        public bool Success { get; set; }
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
