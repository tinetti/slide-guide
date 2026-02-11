using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace SlideGuide.Telemetry.SharedMemory;

/// <summary>
/// Reads data from Windows shared memory (memory-mapped files).
/// </summary>
public class SharedMemoryReader : IDisposable
{
    private MemoryMappedFile? _memoryMappedFile;
    private MemoryMappedViewAccessor? _accessor;
    private bool _disposed;

    /// <summary>
    /// Gets whether the shared memory is currently open.
    /// </summary>
    public bool IsOpen => _memoryMappedFile != null && _accessor != null;

    /// <summary>
    /// Opens the shared memory with the specified name.
    /// </summary>
    /// <param name="mapName">The name of the memory-mapped file.</param>
    /// <returns>True if successfully opened, false otherwise.</returns>
    public bool Open(string mapName)
    {
        try
        {
            Close();

            _memoryMappedFile = MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.Read);
            _accessor = _memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

            return true;
        }
        catch (FileNotFoundException)
        {
            // Memory map doesn't exist (iRacing not running)
            return false;
        }
        catch (Exception)
        {
            // Other errors (permissions, etc.)
            Close();
            return false;
        }
    }

    /// <summary>
    /// Reads a structure from the shared memory at the specified offset.
    /// </summary>
    /// <typeparam name="T">The type of structure to read.</typeparam>
    /// <param name="offset">The offset in bytes.</param>
    /// <param name="value">The value read from memory.</param>
    /// <returns>True if successfully read, false otherwise.</returns>
    public bool Read<T>(int offset, out T value) where T : struct
    {
        value = default;

        if (_accessor == null)
        {
            return false;
        }

        try
        {
            _accessor.Read(offset, out value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Reads a byte array from the shared memory at the specified offset.
    /// </summary>
    /// <param name="offset">The offset in bytes.</param>
    /// <param name="buffer">The buffer to read into.</param>
    /// <returns>The number of bytes read.</returns>
    public int ReadArray(int offset, byte[] buffer)
    {
        if (_accessor == null || buffer == null)
        {
            return 0;
        }

        try
        {
            return _accessor.ReadArray(offset, buffer, 0, buffer.Length);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Closes the shared memory.
    /// </summary>
    public void Close()
    {
        _accessor?.Dispose();
        _accessor = null;

        _memoryMappedFile?.Dispose();
        _memoryMappedFile = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Close();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
