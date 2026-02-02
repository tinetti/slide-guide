namespace IbtTelemetry.Core.Constants;

/// <summary>
/// iRacing SDK Constants - Variable types, flags, and state enumerations
/// </summary>
public static class IrsdkConstants
{
    /// <summary>
    /// Variable data types supported by the iRacing SDK
    /// </summary>
    public enum VarType
    {
        /// <summary>1-byte character/string</summary>
        Char = 0,

        /// <summary>1-byte boolean</summary>
        Bool = 1,

        /// <summary>4-byte signed integer</summary>
        Int = 2,

        /// <summary>4-byte unsigned bit field</summary>
        BitField = 3,

        /// <summary>4-byte floating point</summary>
        Float = 4,

        /// <summary>8-byte double precision floating point</summary>
        Double = 5
    }

    /// <summary>
    /// Engine warning flags
    /// </summary>
    [Flags]
    public enum EngineWarnings
    {
        WaterTempWarning = 0x01,
        FuelPressureWarning = 0x02,
        OilPressureWarning = 0x04,
        EngineStalled = 0x08,
        PitSpeedLimiter = 0x10,
        RevLimiterActive = 0x20,
        OilTempWarning = 0x40
    }

    /// <summary>
    /// Session flags (global and driver-specific)
    /// </summary>
    [Flags]
    public enum SessionFlags
    {
        // Global flags
        Checkered = 0x00000001,
        White = 0x00000002,
        Green = 0x00000004,
        Yellow = 0x00000008,
        Red = 0x00000010,
        Blue = 0x00000020,
        Debris = 0x00000040,
        Crossed = 0x00000080,
        YellowWaving = 0x00000100,
        OneLapToGreen = 0x00000200,
        GreenHeld = 0x00000400,
        TenToGo = 0x00000800,
        FiveToGo = 0x00001000,
        RandomWaving = 0x00002000,
        Caution = 0x00004000,
        CautionWaving = 0x00008000,

        // Driver black flags
        Black = 0x00010000,
        Disqualify = 0x00020000,
        Servicible = 0x00040000,
        Furled = 0x00080000,
        Repair = 0x00100000,

        // Start lights
        StartHidden = 0x10000000,
        StartReady = 0x20000000,
        StartSet = 0x40000000,
        StartGo = unchecked((int)0x80000000)
    }

    /// <summary>
    /// Track location
    /// </summary>
    public enum TrkLoc
    {
        NotInWorld = -1,
        OffTrack = 0,
        InPitStall = 1,
        AproachingPits = 2,
        OnTrack = 3
    }

    /// <summary>
    /// Track surface material
    /// </summary>
    public enum TrkSurf
    {
        SurfaceNotInWorld = -1,
        UndefinedMaterial = 0,

        Asphalt1Material = 1,
        Asphalt2Material = 2,
        Asphalt3Material = 3,
        Asphalt4Material = 4,
        Concrete1Material = 5,
        Concrete2Material = 6,
        RacingDirt1Material = 7,
        RacingDirt2Material = 8,
        Paint1Material = 9,
        Paint2Material = 10,
        Rumble1Material = 11,
        Rumble2Material = 12,
        Rumble3Material = 13,
        Rumble4Material = 14,

        Grass1Material = 15,
        Grass2Material = 16,
        Grass3Material = 17,
        Grass4Material = 18,
        Dirt1Material = 19,
        Dirt2Material = 20,
        Dirt3Material = 21,
        Dirt4Material = 22,
        SandMaterial = 23,
        Gravel1Material = 24,
        Gravel2Material = 25,
        GrasscreteMaterial = 26,
        AstroturfMaterial = 27
    }

    /// <summary>
    /// Session state
    /// </summary>
    public enum SessionState
    {
        Invalid = 0,
        GetInCar = 1,
        Warmup = 2,
        ParadeLaps = 3,
        Racing = 4,
        Checkered = 5,
        CoolDown = 6
    }

    /// <summary>
    /// Car left/right positioning
    /// </summary>
    public enum CarLeftRight
    {
        Off = 0,
        Clear = 1,
        CarLeft = 2,
        CarRight = 3,
        CarLeftRight = 4,
        TwoCarsLeft = 5,
        TwoCarsRight = 6
    }

    /// <summary>
    /// Camera state flags
    /// </summary>
    [Flags]
    public enum CameraState
    {
        IsSessionScreen = 0x0001,
        IsScenicActive = 0x0002,
        CamToolActive = 0x0004,
        UIHidden = 0x0008,
        UseAutoShotSelection = 0x0010,
        UseTemporaryEdits = 0x0020,
        UseKeyAcceleration = 0x0040,
        UseKey10xAcceleration = 0x0080,
        UseMouseAimMode = 0x0100
    }

    /// <summary>
    /// Pit service flags
    /// </summary>
    [Flags]
    public enum PitSvFlags
    {
        LFTireChange = 0x0001,
        RFTireChange = 0x0002,
        LRTireChange = 0x0004,
        RRTireChange = 0x0008,
        FuelFill = 0x0010,
        WindshieldTearoff = 0x0020,
        FastRepair = 0x0040
    }

    /// <summary>
    /// Pit service status
    /// </summary>
    public enum PitSvStatus
    {
        // Status
        None = 0,
        InProgress = 1,
        Complete = 2,

        // Errors
        TooFarLeft = 100,
        TooFarRight = 101,
        TooFarForward = 102,
        TooFarBack = 103,
        BadAngle = 104,
        CantFixThat = 105
    }

    /// <summary>
    /// Pace mode
    /// </summary>
    public enum PaceMode
    {
        SingleFileStart = 0,
        DoubleFileStart = 1,
        SingleFileRestart = 2,
        DoubleFileRestart = 3,
        NotPacing = 4
    }

    /// <summary>
    /// Pace flags
    /// </summary>
    [Flags]
    public enum PaceFlags
    {
        EndOfLine = 0x01,
        FreePass = 0x02,
        WavedAround = 0x04
    }

    /// <summary>
    /// Type information for variable types
    /// </summary>
    public record VarTypeInfo(VarType Type, int Size, string IracingType);

    /// <summary>
    /// Get type information for a variable type
    /// </summary>
    public static VarTypeInfo GetTypeInfo(VarType type) => type switch
    {
        VarType.Char => new VarTypeInfo(VarType.Char, 1, "irsdk_char"),
        VarType.Bool => new VarTypeInfo(VarType.Bool, 1, "irsdk_bool"),
        VarType.Int => new VarTypeInfo(VarType.Int, 4, "irsdk_int"),
        VarType.BitField => new VarTypeInfo(VarType.BitField, 4, "irsdk_bitField"),
        VarType.Float => new VarTypeInfo(VarType.Float, 4, "irsdk_float"),
        VarType.Double => new VarTypeInfo(VarType.Double, 8, "irsdk_double"),
        _ => throw new ArgumentException($"Unknown variable type: {type}", nameof(type))
    };
}
