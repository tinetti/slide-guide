using System;
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

        // Display a few key parameters
        var speed = sample.GetParameter("Speed");
        var rpm = sample.GetParameter("RPM");
        var gear = sample.GetParameter("Gear");
        var throttle = sample.GetParameter("Throttle");
        var brake = sample.GetParameter("Brake");

        if (speed != null)
            Console.WriteLine($"  Speed:    {speed.Value} {speed.Unit}");
        if (rpm != null)
            Console.WriteLine($"  RPM:      {rpm.Value} {rpm.Unit}");
        if (gear != null)
            Console.WriteLine($"  Gear:     {gear.Value}");
        if (throttle != null)
            Console.WriteLine($"  Throttle: {throttle.Value} {throttle.Unit}");
        if (brake != null)
            Console.WriteLine($"  Brake:    {brake.Value} {brake.Unit}");

        Console.WriteLine();
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
