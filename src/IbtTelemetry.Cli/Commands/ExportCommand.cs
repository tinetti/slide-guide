using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IbtTelemetry.Core.Export;
using IbtTelemetry.Core.Services;
using Microsoft.Extensions.Logging;

namespace IbtTelemetry.Cli.Commands;

/// <summary>
/// Command to export telemetry data to Parquet for machine learning
/// </summary>
public class ExportCommand
{
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<ExportCommand> _logger;

    public ExportCommand(
        ITelemetryService telemetryService,
        ILogger<ExportCommand> logger)
    {
        _telemetryService = telemetryService;
        _logger = logger;
    }

    /// <summary>
    /// Execute the export command
    /// </summary>
    public async Task<int> ExecuteAsync(
        string inputPath,
        string outputPath,
        bool allVariables,
        string[]? variables,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exporter = new ParquetExporter();

            // Check if input is a file or directory
            if (File.Exists(inputPath))
            {
                // Single file export
                _logger.LogInformation("Exporting telemetry file: {FilePath}", inputPath);

                using var telemetry = await _telemetryService.LoadFromFileAsync(inputPath, cancellationToken);

                await exporter.ExportAsync(
                    telemetry,
                    outputPath,
                    variables,
                    allVariables,
                    cancellationToken);

                _logger.LogInformation("Exported to: {OutputPath}", outputPath);
                Console.WriteLine($"✓ Exported {telemetry.Header.NumBuf} samples to {outputPath}");
            }
            else if (Directory.Exists(inputPath))
            {
                // Multiple file export
                _logger.LogInformation("Exporting directory: {DirPath}", inputPath);

                var ibtFiles = Directory.GetFiles(inputPath, "*.ibt");
                if (ibtFiles.Length == 0)
                {
                    Console.Error.WriteLine("No .ibt files found in directory");
                    return 1;
                }

                Console.WriteLine($"Found {ibtFiles.Length} .ibt files");

                var progress = new Progress<ExportProgress>(p =>
                {
                    Console.WriteLine($"[{p.FilesProcessed}/{p.TotalFiles}] {p.CurrentFile} ({p.PercentComplete:F1}%)");
                });

                await exporter.ExportMultipleAsync(
                    ibtFiles,
                    outputPath,
                    path => _telemetryService.LoadFromFileAsync(path, cancellationToken),
                    variables,
                    allVariables,
                    progress,
                    cancellationToken);

                _logger.LogInformation("Exported {Count} files to: {OutputPath}", ibtFiles.Length, outputPath);
                Console.WriteLine($"✓ Exported {ibtFiles.Length} sessions to {outputPath}");
            }
            else
            {
                Console.Error.WriteLine($"Error: {inputPath} is not a valid file or directory");
                return 1;
            }

            // Show variable info
            if (allVariables)
            {
                Console.WriteLine("Exported all variables");
            }
            else if (variables != null && variables.Length > 0)
            {
                Console.WriteLine($"Exported {variables.Length} custom variables");
            }
            else
            {
                Console.WriteLine($"Exported {ParquetExporter.DefaultMlVariables.Length} default ML variables");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting telemetry data");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// List available variables
    /// </summary>
    public async Task<int> ListVariablesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var telemetry = await _telemetryService.LoadFromFileAsync(filePath, cancellationToken);

            Console.WriteLine($"\nAvailable variables ({telemetry.Header.NumVars} total):\n");

            // Group by type
            var varsByType = telemetry.VarHeaders
                .GroupBy(v => v.Type)
                .OrderBy(g => g.Key);

            foreach (var group in varsByType)
            {
                Console.WriteLine($"{group.Key} ({group.Count()} variables):");
                foreach (var varHeader in group.OrderBy(v => v.Name))
                {
                    var unitStr = string.IsNullOrWhiteSpace(varHeader.Unit) ? "" : $" [{varHeader.Unit}]";
                    var countStr = varHeader.Count > 1 ? $" (array[{varHeader.Count}])" : "";
                    Console.WriteLine($"  {varHeader.Name}{countStr}{unitStr}");
                }
                Console.WriteLine();
            }

            Console.WriteLine("\nDefault ML variables:");
            foreach (var varName in ParquetExporter.DefaultMlVariables)
            {
                Console.WriteLine($"  {varName}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing variables");
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
