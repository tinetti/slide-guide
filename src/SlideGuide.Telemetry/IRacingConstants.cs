namespace SlideGuide.Telemetry;

/// <summary>
/// Constants for iRacing shared memory integration.
/// </summary>
public static class IRacingConstants
{
    /// <summary>
    /// Name of the iRacing shared memory file for telemetry data.
    /// </summary>
    public const string MemoryMapName = "Local\\IRSDKMemMapFileName";

    /// <summary>
    /// Default polling interval in milliseconds.
    /// </summary>
    public const int DefaultPollingIntervalMs = 16; // ~60 Hz

    /// <summary>
    /// Size of the header in bytes (based on iRacing SDK).
    /// </summary>
    public const int HeaderSize = 112;
}
