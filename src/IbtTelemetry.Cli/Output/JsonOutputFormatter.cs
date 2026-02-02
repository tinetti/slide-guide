using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using IbtTelemetry.Core.Models;

namespace IbtTelemetry.Cli.Output;

/// <summary>
/// Formats telemetry output as JSON
/// </summary>
public class JsonOutputFormatter : IOutputFormatter
{
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _firstSample = true;

    public string FormatName => "json";

    public JsonOutputFormatter()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public void DisplaySessionInfo(Telemetry telemetry)
    {
        Console.WriteLine("{");
        Console.WriteLine("  \"header\": {");
        Console.WriteLine($"    \"version\": {telemetry.Header.Version},");
        Console.WriteLine($"    \"tickRate\": {telemetry.Header.TickRate},");
        Console.WriteLine($"    \"numVars\": {telemetry.Header.NumVars},");
        Console.WriteLine($"    \"numBuf\": {telemetry.Header.NumBuf},");
        Console.WriteLine($"    \"bufLen\": {telemetry.Header.BufLen}");
        Console.WriteLine("  },");

        Console.WriteLine("  \"diskHeader\": {");
        Console.WriteLine($"    \"startDate\": {telemetry.DiskHeader.StartDate},");
        Console.WriteLine($"    \"startTime\": {telemetry.DiskHeader.StartTime},");
        Console.WriteLine($"    \"endTime\": {telemetry.DiskHeader.EndTime},");
        Console.WriteLine($"    \"lapCount\": {telemetry.DiskHeader.LapCount},");
        Console.WriteLine($"    \"recordCount\": {telemetry.DiskHeader.RecordCount}");
        Console.WriteLine("  },");

        Console.WriteLine("  \"sessionInfo\": {");

        // Serialize session info
        if (telemetry.SessionInfo.WeekendInfo != null)
        {
            var weekendJson = JsonSerializer.Serialize(telemetry.SessionInfo.WeekendInfo, _jsonOptions);
            Console.WriteLine($"    \"weekendInfo\": {weekendJson},");
        }

        if (telemetry.SessionInfo.DriverInfo != null)
        {
            var driverJson = JsonSerializer.Serialize(telemetry.SessionInfo.DriverInfo, _jsonOptions);
            Console.WriteLine($"    \"driverInfo\": {driverJson}");
        }

        Console.WriteLine("  }");
    }

    public void DisplaySampleHeader()
    {
        Console.WriteLine("  ,\"samples\": [");
        _firstSample = true;
    }

    public void DisplaySample(TelemetrySample sample, int sampleNumber)
    {
        if (!_firstSample)
        {
            Console.WriteLine(",");
        }
        _firstSample = false;

        var sampleData = sample.ToJsonDictionary();
        var json = JsonSerializer.Serialize(sampleData, _jsonOptions);

        // Indent the JSON
        var indentedJson = IndentJson(json, 4);
        Console.Write(indentedJson);
    }

    public void DisplaySampleFooter()
    {
        Console.WriteLine();
        Console.WriteLine("  ]");
        Console.WriteLine("}");
    }

    private string IndentJson(string json, int spaces)
    {
        var lines = json.Split('\n');
        var indented = new System.Text.StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                indented.Append('\n');

            indented.Append(new string(' ', spaces));
            indented.Append(lines[i]);
        }

        return indented.ToString();
    }
}
