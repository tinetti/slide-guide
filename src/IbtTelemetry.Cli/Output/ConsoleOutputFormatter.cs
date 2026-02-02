using System;
using System.Linq;
using IbtTelemetry.Core.Models;

namespace IbtTelemetry.Cli.Output;

/// <summary>
/// Formats telemetry output for console display
/// </summary>
public class ConsoleOutputFormatter : IOutputFormatter
{
    public string FormatName => "console";

    public void DisplaySessionInfo(Telemetry telemetry)
    {
        Console.WriteLine("=== Telemetry File Information ===");
        Console.WriteLine();

        Console.WriteLine($"SDK Version:     {telemetry.Header.Version}");
        Console.WriteLine($"Tick Rate:       {telemetry.Header.TickRate} Hz");
        Console.WriteLine($"Variables:       {telemetry.Header.NumVars}");
        Console.WriteLine($"Sample Buffers:  {telemetry.Header.NumBuf}");
        Console.WriteLine($"Buffer Size:     {telemetry.Header.BufLen} bytes");
        Console.WriteLine();

        Console.WriteLine("=== Session Information ===");
        Console.WriteLine();

        // Display weekend info
        if (telemetry.SessionInfo.WeekendInfo != null)
        {
            DisplayDictionary("Weekend", telemetry.SessionInfo.WeekendInfo);
        }

        // Display driver info
        if (telemetry.SessionInfo.DriverInfo != null)
        {
            DisplayDictionary("Driver", telemetry.SessionInfo.DriverInfo);
        }

        Console.WriteLine();

        // Display variable summary
        Console.WriteLine("=== Telemetry Variables ===");
        Console.WriteLine();
        Console.WriteLine($"Total Variables: {telemetry.VarHeaders.Count}");
        Console.WriteLine();

        // Group variables by type
        var varsByType = telemetry.VarHeaders
            .GroupBy(v => v.Type)
            .OrderBy(g => g.Key);

        foreach (var group in varsByType)
        {
            Console.WriteLine($"  {group.Key,-10} {group.Count(),3} variables");
        }

        Console.WriteLine();
    }

    public void DisplaySampleHeader()
    {
        Console.WriteLine();
        Console.WriteLine("=== Telemetry Samples ===");
        Console.WriteLine();
    }

    public void DisplaySample(TelemetrySample sample, int sampleNumber)
    {
        Console.WriteLine($"--- Sample #{sampleNumber} ---");

        // Get all parameters
        var allParams = sample.ToJsonDictionary();

        // Display all parameters
        foreach (var param in allParams)
        {
            var paramName = param.Key;
            var paramData = param.Value;

            if (paramData.TryGetValue("value", out var value) && value != null)
            {
                var unit = paramData.TryGetValue("unit", out var u) && u != null ? u.ToString() : "";
                var formattedValue = FormatValue(value);

                if (!string.IsNullOrWhiteSpace(unit))
                {
                    Console.WriteLine($"  {paramName,-30} {formattedValue} {unit}");
                }
                else
                {
                    Console.WriteLine($"  {paramName,-30} {formattedValue}");
                }
            }
        }

        Console.WriteLine();
    }

    private string FormatValue(object value)
    {
        return value switch
        {
            float f => f.ToString("F3"),
            double d => d.ToString("F3"),
            float[] fa => "[" + string.Join(", ", fa.Select(x => x.ToString("F3"))) + "]",
            double[] da => "[" + string.Join(", ", da.Select(x => x.ToString("F3"))) + "]",
            int[] ia => "[" + string.Join(", ", ia) + "]",
            uint[] ua => "[" + string.Join(", ", ua) + "]",
            bool[] ba => "[" + string.Join(", ", ba) + "]",
            _ => value.ToString() ?? ""
        };
    }

    public void DisplaySampleFooter()
    {
        Console.WriteLine("=== End of Samples ===");
    }

    private void DisplayDictionary(string section, System.Collections.Generic.Dictionary<string, object> dict, int indent = 0)
    {
        var prefix = new string(' ', indent * 2);

        foreach (var kvp in dict)
        {
            if (kvp.Value is System.Collections.Generic.Dictionary<string, object> nestedDict)
            {
                Console.WriteLine($"{prefix}{kvp.Key}:");
                DisplayDictionary(section, nestedDict, indent + 1);
            }
            else if (kvp.Value is System.Collections.IEnumerable enumerable && kvp.Value is not string)
            {
                Console.WriteLine($"{prefix}{kvp.Key}: [array]");
            }
            else
            {
                Console.WriteLine($"{prefix}{kvp.Key}: {kvp.Value}");
            }
        }
    }
}
