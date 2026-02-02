using System.Collections.Generic;

namespace IbtTelemetry.Core.Models;

/// <summary>
/// Session information parsed from YAML
/// Contains weekend info, driver info, and session details
/// </summary>
public class SessionInfo
{
    /// <summary>Weekend information</summary>
    public Dictionary<string, object>? WeekendInfo { get; set; }

    /// <summary>Driver information</summary>
    public Dictionary<string, object>? DriverInfo { get; set; }

    /// <summary>Session information array</summary>
    public List<Dictionary<string, object>>? SessionInfoData { get; set; }

    /// <summary>Camera information</summary>
    public Dictionary<string, object>? CameraInfo { get; set; }

    /// <summary>Radio information</summary>
    public Dictionary<string, object>? RadioInfo { get; set; }

    /// <summary>Split information</summary>
    public Dictionary<string, object>? SplitTimeInfo { get; set; }

    /// <summary>
    /// Additional properties for forward compatibility
    /// </summary>
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();

    /// <summary>
    /// Get a value from WeekendInfo safely
    /// </summary>
    public T? GetWeekendValue<T>(string key)
    {
        if (WeekendInfo != null && WeekendInfo.TryGetValue(key, out var value))
        {
            return (T?)value;
        }
        return default;
    }

    /// <summary>
    /// Get a value from DriverInfo safely
    /// </summary>
    public T? GetDriverValue<T>(string key)
    {
        if (DriverInfo != null && DriverInfo.TryGetValue(key, out var value))
        {
            return (T?)value;
        }
        return default;
    }
}
