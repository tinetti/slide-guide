using System.Runtime.InteropServices;

namespace SlideGuide.Telemetry.Models;

/// <summary>
/// Represents the header structure of the iRacing shared memory.
/// Based on the iRacing SDK documentation.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct IRacingHeader
{
    public int Version;
    public int Status;
    public int TickRate;

    public int SessionInfoUpdate;
    public int SessionInfoLen;
    public int SessionInfoOffset;

    public int NumVars;
    public int VarHeaderOffset;

    public int NumBuf;
    public int BufLen;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public int[] Padding1;

    public int VarBuf1Offset;
    public int VarBuf2Offset;
    public int VarBuf3Offset;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
    public int[] Padding2;
}
