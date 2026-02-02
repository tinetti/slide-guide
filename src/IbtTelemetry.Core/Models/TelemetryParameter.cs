namespace IbtTelemetry.Core.Models;

/// <summary>
/// Represents a single telemetry parameter with its metadata
/// </summary>
/// <param name="Name">Parameter name</param>
/// <param name="Description">Parameter description</param>
/// <param name="Value">Parameter value</param>
/// <param name="Unit">Unit of measurement</param>
public record TelemetryParameter(
    string Name,
    string Description,
    object? Value,
    string Unit
);
