using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using IbtTelemetry.Core.Models;
using IbtTelemetry.Core.Models.Headers;

namespace IbtTelemetry.Core.Export;

/// <summary>
/// Exports telemetry data to Parquet format for machine learning
/// </summary>
public class ParquetExporter
{
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
        "YawRate",

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
    /// Export telemetry to Parquet with selected variables
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

        // Collect all samples first
        var samples = new List<TelemetrySample>();
        await foreach (var sample in telemetry.GetSamplesAsync(cancellationToken))
        {
            samples.Add(sample);
        }

        // Build schema and write data
        var sessionId = telemetry.GetUniqueId();
        await WriteParquetFile(outputPath, sessionId, samples, validVariables, telemetry.VarHeaders);
    }

    /// <summary>
    /// Export multiple telemetry files to a single Parquet file
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

        var allSamples = new List<(string sessionId, TelemetrySample sample, int sampleIdx)>();
        string[]? validVariables = null;
        IReadOnlyList<VarHeader>? varHeaders = null;

        foreach (var filePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var telemetry = await loadTelemetryFunc(filePath);

            var variables = includeAllVariables
                ? telemetry.VarHeaders.Select(v => v.Name).ToArray()
                : selectedVariables ?? DefaultMlVariables;

            if (validVariables == null)
            {
                validVariables = variables
                    .Where(v => telemetry.VarHeaders.Any(h => h.Name == v))
                    .ToArray();
                varHeaders = telemetry.VarHeaders;
            }

            var sessionId = telemetry.GetUniqueId();
            var sampleIdx = 0;

            await foreach (var sample in telemetry.GetSamplesAsync(cancellationToken))
            {
                allSamples.Add((sessionId, sample, sampleIdx));
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

        if (validVariables != null && varHeaders != null)
        {
            await WriteParquetFileMultiple(outputPath, allSamples, validVariables, varHeaders);
        }
    }

    private async Task WriteParquetFile(
        string outputPath,
        string sessionId,
        List<TelemetrySample> samples,
        string[] variables,
        IReadOnlyList<VarHeader> varHeaders)
    {
        var fields = new List<Field>
        {
            new Field("session_id", StringType.Default, nullable: false),
            new Field("sample_idx", Int32Type.Default, nullable: false)
        };

        // Add fields for each variable
        foreach (var varName in variables)
        {
            var varHeader = varHeaders.First(h => h.Name == varName);
            var field = CreateFieldForVariable(varName, varHeader);
            fields.Add(field);
        }

        var schema = new Schema(fields, null);

        using var stream = File.Create(outputPath);
        using var writer = new ArrowFileWriter(stream, schema);

        await writer.WriteRecordBatchAsync(
            CreateRecordBatch(schema, sessionId, samples, variables, varHeaders),
            cancellationToken: default);
    }

    private async Task WriteParquetFileMultiple(
        string outputPath,
        List<(string sessionId, TelemetrySample sample, int sampleIdx)> allSamples,
        string[] variables,
        IReadOnlyList<VarHeader> varHeaders)
    {
        var fields = new List<Field>
        {
            new Field("session_id", StringType.Default, nullable: false),
            new Field("sample_idx", Int32Type.Default, nullable: false)
        };

        foreach (var varName in variables)
        {
            var varHeader = varHeaders.First(h => h.Name == varName);
            var field = CreateFieldForVariable(varName, varHeader);
            fields.Add(field);
        }

        var schema = new Schema(fields, null);

        using var stream = File.Create(outputPath);
        using var writer = new ArrowFileWriter(stream, schema);

        await writer.WriteRecordBatchAsync(
            CreateRecordBatchMultiple(schema, allSamples, variables, varHeaders),
            cancellationToken: default);
    }

    private Field CreateFieldForVariable(string varName, VarHeader varHeader)
    {
        // For arrays, we'll flatten to individual columns or store as string representation
        // For simplicity, storing most recent value for arrays
        IArrowType arrowType = varHeader.Type switch
        {
            Constants.IrsdkConstants.VarType.Char => StringType.Default,
            Constants.IrsdkConstants.VarType.Bool => BooleanType.Default,
            Constants.IrsdkConstants.VarType.Int => Int32Type.Default,
            Constants.IrsdkConstants.VarType.BitField => UInt32Type.Default,
            Constants.IrsdkConstants.VarType.Float => FloatType.Default,
            Constants.IrsdkConstants.VarType.Double => DoubleType.Default,
            _ => StringType.Default
        };

        return new Field(varName, arrowType, nullable: true);
    }

    private RecordBatch CreateRecordBatch(
        Schema schema,
        string sessionId,
        List<TelemetrySample> samples,
        string[] variables,
        IReadOnlyList<VarHeader> varHeaders)
    {
        var arrays = new List<IArrowArray>();

        // Session ID column
        var sessionIds = Enumerable.Repeat(sessionId, samples.Count).ToArray();
        arrays.Add(new StringArray.Builder().AppendRange(sessionIds).Build());

        // Sample index column
        var indices = Enumerable.Range(0, samples.Count).ToArray();
        arrays.Add(new Int32Array.Builder().AppendRange(indices).Build());

        // Variable columns
        foreach (var varName in variables)
        {
            var varHeader = varHeaders.First(h => h.Name == varName);
            arrays.Add(CreateArrayForVariable(samples, varName, varHeader));
        }

        return new RecordBatch(schema, arrays, samples.Count);
    }

    private RecordBatch CreateRecordBatchMultiple(
        Schema schema,
        List<(string sessionId, TelemetrySample sample, int sampleIdx)> allSamples,
        string[] variables,
        IReadOnlyList<VarHeader> varHeaders)
    {
        var arrays = new List<IArrowArray>();

        // Session ID column
        var sessionIds = allSamples.Select(s => s.sessionId).ToArray();
        arrays.Add(new StringArray.Builder().AppendRange(sessionIds).Build());

        // Sample index column
        var indices = allSamples.Select(s => s.sampleIdx).ToArray();
        arrays.Add(new Int32Array.Builder().AppendRange(indices).Build());

        // Variable columns
        foreach (var varName in variables)
        {
            var varHeader = varHeaders.First(h => h.Name == varName);
            arrays.Add(CreateArrayForVariableMultiple(allSamples, varName, varHeader));
        }

        return new RecordBatch(schema, arrays, allSamples.Count);
    }

    private IArrowArray CreateArrayForVariable(
        List<TelemetrySample> samples,
        string varName,
        VarHeader varHeader)
    {
        return varHeader.Type switch
        {
            Constants.IrsdkConstants.VarType.Bool => CreateBoolArray(samples, varName),
            Constants.IrsdkConstants.VarType.Int => CreateInt32Array(samples, varName),
            Constants.IrsdkConstants.VarType.BitField => CreateUInt32Array(samples, varName),
            Constants.IrsdkConstants.VarType.Float => CreateFloatArray(samples, varName),
            Constants.IrsdkConstants.VarType.Double => CreateDoubleArray(samples, varName),
            _ => CreateStringArray(samples, varName)
        };
    }

    private IArrowArray CreateArrayForVariableMultiple(
        List<(string sessionId, TelemetrySample sample, int sampleIdx)> allSamples,
        string varName,
        VarHeader varHeader)
    {
        var samples = allSamples.Select(s => s.sample).ToList();
        return CreateArrayForVariable(samples, varName, varHeader);
    }

    private IArrowArray CreateBoolArray(List<TelemetrySample> samples, string varName)
    {
        var builder = new BooleanArray.Builder();
        foreach (var sample in samples)
        {
            var param = sample.GetParameter(varName);
            if (param?.Value is bool b)
                builder.Append(b);
            else
                builder.AppendNull();
        }
        return builder.Build();
    }

    private IArrowArray CreateInt32Array(List<TelemetrySample> samples, string varName)
    {
        var builder = new Int32Array.Builder();
        foreach (var sample in samples)
        {
            var param = sample.GetParameter(varName);
            if (param?.Value is int i)
                builder.Append(i);
            else
                builder.AppendNull();
        }
        return builder.Build();
    }

    private IArrowArray CreateUInt32Array(List<TelemetrySample> samples, string varName)
    {
        var builder = new UInt32Array.Builder();
        foreach (var sample in samples)
        {
            var param = sample.GetParameter(varName);
            if (param?.Value is uint ui)
                builder.Append(ui);
            else
                builder.AppendNull();
        }
        return builder.Build();
    }

    private IArrowArray CreateFloatArray(List<TelemetrySample> samples, string varName)
    {
        var builder = new FloatArray.Builder();
        foreach (var sample in samples)
        {
            var param = sample.GetParameter(varName);
            var value = param?.Value;

            if (value is float f)
                builder.Append(f);
            else if (value is float[] fa && fa.Length > 0)
                builder.Append(fa[^1]); // Use last value from array
            else
                builder.AppendNull();
        }
        return builder.Build();
    }

    private IArrowArray CreateDoubleArray(List<TelemetrySample> samples, string varName)
    {
        var builder = new DoubleArray.Builder();
        foreach (var sample in samples)
        {
            var param = sample.GetParameter(varName);
            var value = param?.Value;

            if (value is double d)
                builder.Append(d);
            else if (value is double[] da && da.Length > 0)
                builder.Append(da[^1]); // Use last value from array
            else
                builder.AppendNull();
        }
        return builder.Build();
    }

    private IArrowArray CreateStringArray(List<TelemetrySample> samples, string varName)
    {
        var builder = new StringArray.Builder();
        foreach (var sample in samples)
        {
            var param = sample.GetParameter(varName);
            if (param?.Value != null)
                builder.Append(param.Value.ToString());
            else
                builder.AppendNull();
        }
        return builder.Build();
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
