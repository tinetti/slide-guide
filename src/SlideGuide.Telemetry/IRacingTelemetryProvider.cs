using SlideGuide.Core.Interfaces;
using SlideGuide.Core.Models;
using SlideGuide.Telemetry.SharedMemory;
using SlideGuide.Telemetry.Models;

namespace SlideGuide.Telemetry;

/// <summary>
/// Provides telemetry data from iRacing via shared memory.
/// </summary>
public class IRacingTelemetryProvider : ITelemetryProvider
{
    private readonly SharedMemoryReader _memoryReader;
    private readonly int _pollingIntervalMs;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _pollingTask;
    private TelemetryData _currentData;
    private bool _isConnected;

    /// <inheritdoc/>
    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                ConnectionStatusChanged?.Invoke(this, value);
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<TelemetryData>? TelemetryUpdated;

    /// <inheritdoc/>
    public event EventHandler<bool>? ConnectionStatusChanged;

    /// <summary>
    /// Creates a new instance of the iRacing telemetry provider.
    /// </summary>
    /// <param name="pollingIntervalMs">The polling interval in milliseconds (default: 16ms for ~60 Hz).</param>
    public IRacingTelemetryProvider(int pollingIntervalMs = IRacingConstants.DefaultPollingIntervalMs)
    {
        _memoryReader = new SharedMemoryReader();
        _pollingIntervalMs = pollingIntervalMs;
        _currentData = TelemetryData.Empty;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_pollingTask != null)
        {
            return; // Already running
        }

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _pollingTask = Task.Run(() => PollTelemetryAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
        }

        if (_pollingTask != null)
        {
            try
            {
                await _pollingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }

            _pollingTask = null;
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _memoryReader.Close();
        IsConnected = false;
    }

    /// <inheritdoc/>
    public TelemetryData GetCurrentData()
    {
        return _currentData;
    }

    private async Task PollTelemetryAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Try to connect to shared memory if not connected
                if (!_memoryReader.IsOpen)
                {
                    var connected = _memoryReader.Open(IRacingConstants.MemoryMapName);
                    if (!connected)
                    {
                        IsConnected = false;
                        await Task.Delay(_pollingIntervalMs * 10, cancellationToken); // Wait longer when not connected
                        continue;
                    }
                }

                // Read telemetry data
                var telemetryData = ReadTelemetryData();
                if (telemetryData != null)
                {
                    _currentData = telemetryData;
                    IsConnected = true;
                    TelemetryUpdated?.Invoke(this, telemetryData);
                }
                else
                {
                    // Lost connection
                    _memoryReader.Close();
                    IsConnected = false;
                }

                await Task.Delay(_pollingIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Log error in production, for now just continue
                _memoryReader.Close();
                IsConnected = false;
                await Task.Delay(_pollingIntervalMs * 10, cancellationToken);
            }
        }
    }

    private TelemetryData? ReadTelemetryData()
    {
        // Read header to verify connection
        if (!_memoryReader.Read(0, out IRacingHeader header))
        {
            return null;
        }

        // Basic validation
        if (header.Status == 0 || header.NumVars == 0)
        {
            return null;
        }

        // For now, return a basic telemetry data object
        // In a full implementation, we would parse the variable data based on the header offsets
        // This is simplified for the initial implementation
        var data = new TelemetryData
        {
            Timestamp = DateTime.UtcNow,
            IsConnected = true,
            IsOnTrack = header.Status == 1,
            // Additional telemetry values would be read from the variable buffer
            // based on the header.VarBuf1Offset, VarBuf2Offset, etc.
        };

        // TODO: Parse actual telemetry variables (throttle, brake, etc.)
        // This requires reading the variable headers and then the data buffers
        // For the initial implementation, we'll use placeholder values

        return data;
    }
}
