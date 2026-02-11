namespace SlideGuide.Core.Models;

/// <summary>
/// Represents a snapshot of telemetry data from the sim racing application.
/// </summary>
public class TelemetryData
{
    /// <summary>
    /// Gets or sets the timestamp when this data was captured.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets whether the sim is currently connected.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Gets or sets the throttle position (0.0 to 1.0).
    /// </summary>
    public float Throttle { get; set; }

    /// <summary>
    /// Gets or sets the brake position (0.0 to 1.0).
    /// </summary>
    public float Brake { get; set; }

    /// <summary>
    /// Gets or sets the clutch position (0.0 to 1.0).
    /// </summary>
    public float Clutch { get; set; }

    /// <summary>
    /// Gets or sets the steering angle in radians.
    /// </summary>
    public float SteeringAngle { get; set; }

    /// <summary>
    /// Gets or sets the current speed in meters per second.
    /// </summary>
    public float Speed { get; set; }

    /// <summary>
    /// Gets or sets the current gear (-1 for reverse, 0 for neutral, 1+ for forward gears).
    /// </summary>
    public int Gear { get; set; }

    /// <summary>
    /// Gets or sets the engine RPM.
    /// </summary>
    public float Rpm { get; set; }

    /// <summary>
    /// Gets or sets the lateral G-force.
    /// </summary>
    public float LateralG { get; set; }

    /// <summary>
    /// Gets or sets the longitudinal G-force.
    /// </summary>
    public float LongitudinalG { get; set; }

    /// <summary>
    /// Gets or sets whether the car is currently on track.
    /// </summary>
    public bool IsOnTrack { get; set; }

    /// <summary>
    /// Creates a new instance with default values representing no telemetry.
    /// </summary>
    public static TelemetryData Empty => new()
    {
        IsConnected = false
    };
}
