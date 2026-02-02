using IbtTelemetry.Core.Models;

namespace IbtTelemetry.Cli.Output;

/// <summary>
/// Interface for formatting telemetry output
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Name of the format (e.g., "json", "console")
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// Display session information
    /// </summary>
    void DisplaySessionInfo(Telemetry telemetry);

    /// <summary>
    /// Display a telemetry sample
    /// </summary>
    void DisplaySample(TelemetrySample sample, int sampleNumber);

    /// <summary>
    /// Display summary header before samples
    /// </summary>
    void DisplaySampleHeader();

    /// <summary>
    /// Display summary footer after samples
    /// </summary>
    void DisplaySampleFooter();
}
