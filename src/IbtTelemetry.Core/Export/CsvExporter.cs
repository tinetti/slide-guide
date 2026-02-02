using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IbtTelemetry.Core.Models;

namespace IbtTelemetry.Core.Export;

/// <summary>
/// Exports telemetry data to CSV format for machine learning
/// </summary>
public class CsvExporter
{
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

    /// <summary>
    /// Default variables for ML training
    /// </summary>
    public static readonly string[] DefaultMlVariables = new[]
    {
        // Time and session
        "SessionTime",
        "Lap",
        "LapDistPct",
        "LapCurrentLapTime",

        // Vehicle dynamics
        "Speed",
        "RPM",
        "Gear",

        // Driver inputs
        "Throttle",
        "Brake",
        "Clutch",
        "SteeringWheelAngle",

        // Accelerations
        "LatAccel",
        "LongAccel",
        "VertAccel",

        // Position and orientation
        "YawNorth",
        "Pitch",
        "Roll",

        // Tire temperatures (outer)
        "LFtempCL",
        "LFtempCM",
        "LFtempCR",
        "RFtempCL",
        "RFtempCM",
        "RFtempCR",
        "LRtempCL",
        "LRtempCM",
        "LRtempCR",
        "RRtempCL",
        "RRtempCM",
        "RRtempCR",

        // Tire wear
        "LFwearL",
        "LFwearM",
        "LFwearR",
        "RFwearL",
        "RFwearM",
        "RFwearR",

        // Tire pressure
        "LFpressure",
        "RFpressure",
        "LRpressure",
        "RRpressure",

        // Fuel
        "FuelLevel",
        "FuelLevelPct",

        // Track conditions
        "TrackTemp",
        "TrackTempCrew"
    };

    /// <summary>
    /// Export telemetry to CSV with selected variables
    /// </summary>
    public async Task ExportAsync(
        Telemetry telemetry,
        string outputPath,
        string[]? selectedVariables = null,
        bool includeAllVariables = false,
        CancellationToken cancellationToken = default)
    {
        var variables = includeAllVariables
            ? telemetry.VarHeaders.Select(v => v.Name).ToArray()
            : selectedVariables ?? DefaultMlVariables;

        // Validate variables exist
        var validVariables = variables
            .Where(v => telemetry.VarHeaders.Any(h => h.Name == v))
            .ToArray();

        if (validVariables.Length == 0)
        {
            throw new ArgumentException("No valid variables found to export");
        }

        await using var writer = new StreamWriter(outputPath);

        // Write header
        var sessionId = telemetry.GetUniqueId();
        var header = string.Join(",", "session_id", "sample_idx", string.Join(",", validVariables));
        await writer.WriteLineAsync(header);

        // Write samples
        var sampleIdx = 0;
        await foreach (var sample in telemetry.GetSamplesAsync(cancellationToken))
        {
            var values = new List<string>
            {
                sessionId,
                sampleIdx.ToString(_culture)
            };

            foreach (var varName in validVariables)
            {
                var param = sample.GetParameter(varName);
                values.Add(FormatValue(param?.Value));
            }

            await writer.WriteLineAsync(string.Join(",", values));
            sampleIdx++;
        }
    }

    /// <summary>
    /// Export multiple telemetry files to a single CSV
    /// </summary>
    public async Task ExportMultipleAsync(
        IEnumerable<string> ibtFilePaths,
        string outputPath,
        Func<string, Task<Telemetry>> loadTelemetryFunc,
        string[]? selectedVariables = null,
        bool includeAllVariables = false,
        IProgress<ExportProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var filePaths = ibtFilePaths.ToArray();
        var totalFiles = filePaths.Length;
        var processedFiles = 0;

        await using var writer = new StreamWriter(outputPath);
        var headerWritten = false;

        foreach (var filePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var telemetry = await loadTelemetryFunc(filePath);

            var variables = includeAllVariables
                ? telemetry.VarHeaders.Select(v => v.Name).ToArray()
                : selectedVariables ?? DefaultMlVariables;

            var validVariables = variables
                .Where(v => telemetry.VarHeaders.Any(h => h.Name == v))
                .ToArray();

            // Write header once
            if (!headerWritten)
            {
                var header = string.Join(",", "session_id", "sample_idx", string.Join(",", validVariables));
                await writer.WriteLineAsync(header);
                headerWritten = true;
            }

            // Write samples
            var sessionId = telemetry.GetUniqueId();
            var sampleIdx = 0;

            await foreach (var sample in telemetry.GetSamplesAsync(cancellationToken))
            {
                var values = new List<string>
                {
                    sessionId,
                    sampleIdx.ToString(_culture)
                };

                foreach (var varName in validVariables)
                {
                    var param = sample.GetParameter(varName);
                    values.Add(FormatValue(param?.Value));
                }

                await writer.WriteLineAsync(string.Join(",", values));
                sampleIdx++;
            }

            processedFiles++;
            progress?.Report(new ExportProgress
            {
                FilesProcessed = processedFiles,
                TotalFiles = totalFiles,
                CurrentFile = Path.GetFileName(filePath)
            });
        }
    }

    private string FormatValue(object? value)
    {
        return value switch
        {
            null => "",
            float f => f.ToString("F6", _culture),
            double d => d.ToString("F6", _culture),
            int i => i.ToString(_culture),
            uint ui => ui.ToString(_culture),
            bool b => b ? "1" : "0",
            string s => EscapeCsvValue(s),
            float[] fa => $"\"[{string.Join(";", fa.Select(x => x.ToString("F6", _culture)))}]\"",
            double[] da => $"\"[{string.Join(";", da.Select(x => x.ToString("F6", _culture)))}]\"",
            int[] ia => $"\"[{string.Join(";", ia)}]\"",
            uint[] ua => $"\"[{string.Join(";", ua)}]\"",
            bool[] ba => $"\"[{string.Join(";", ba.Select(x => x ? "1" : "0"))}]\"",
            _ => EscapeCsvValue(value.ToString() ?? "")
        };
    }

    private string EscapeCsvValue(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}

/// <summary>
/// Progress information for multi-file export
/// </summary>
public record ExportProgress
{
    public int FilesProcessed { get; init; }
    public int TotalFiles { get; init; }
    public string CurrentFile { get; init; } = string.Empty;
    public double PercentComplete => TotalFiles > 0 ? (double)FilesProcessed / TotalFiles * 100 : 0;
}
