using System.Runtime.InteropServices;

namespace MyPiUI.Input;

public abstract class InputDeviceReader<TEvent> where TEvent : struct
{
    protected readonly string DevicePath;
    private FileStream? _deviceStream;
    private Task? _eventTask;
    private CancellationTokenSource? _cts;
    protected readonly object LockObject = new();

    protected InputDeviceReader(string devicePath)
    {
        DevicePath = devicePath;
    }

    public virtual void Initialize()
    {
        if (!File.Exists(DevicePath))
            throw new Exception($"Device not found: {DevicePath}");

        try
        {
            _deviceStream = new FileStream(
                DevicePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
        }
        catch (UnauthorizedAccessException)
        {
            throw new Exception($"Permission denied. Try running: sudo usermod -aG input $USER");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to open device: {ex.Message}");
        }
    }

    public void StartEventLoop()
    {
        if (_deviceStream is null)
            throw new InvalidOperationException("Device not initialized.");
            
        _cts = new CancellationTokenSource();
        _eventTask = Task.Run(() => ProcessEventsAsync(_cts.Token));
    }

    public void StopEventLoop()
    {
        _cts?.Cancel();
        _eventTask?.Wait();
        _cts?.Dispose();
    }

    private async Task ProcessEventsAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[Marshal.SizeOf(typeof(TEvent))];
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                int bytesRead = await _deviceStream!.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead == buffer.Length)
                {
                    var inputEvent = ByteArrayToStructure<TEvent>(buffer);
                    OnInputEvent(inputEvent);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                break;
            }
        }
    }

    protected abstract void OnInputEvent(TEvent inputEvent);

    protected static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
    {
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        T structure = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        handle.Free();
        return structure;
    }
} 