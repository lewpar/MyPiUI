namespace MyKUIPi.Input;

public class InputManager : IDisposable
{
    public static InputManager? Instance { get; set; }
    
    private static int _screenWidth;
    private static int _screenHeight;
    
    private KeyboardReader? _keyReader;
    private MouseReader? _mouseReader;
    private TouchReader? _touchReader;

    public InputManager(MyEngineOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.KeyboardDevice))
        {
            _keyReader = new KeyboardReader(options.KeyboardDevice);
        }

        if (!string.IsNullOrWhiteSpace(options.MouseDevice))
        {
            _mouseReader = new MouseReader(options.MouseDevice);
        }

        if (!string.IsNullOrWhiteSpace(options.TouchDevice))
        {
            _touchReader = new TouchReader(options.TouchDevice, options.MaxTouchX, options.MaxTouchY);
        }

        if (Instance is null)
        {
            Instance = this;
        }
    }

    public void Initialize(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        
        _keyReader?.Initialize();
        _keyReader?.StartEventLoop();

        _mouseReader?.Initialize();
        _mouseReader?.StartEventLoop();
        
        _touchReader?.Initialize();
        _touchReader?.StartEventLoop();
    }

    public bool IsKeyDown(KeyCodes keyCode)
    {
        return _keyReader is null ? false : _keyReader.IsKeyDown(keyCode);
    }

    public (int dx, int dy, int wheel) GetMouseDelta()
    {
        return _mouseReader?.GetAndResetDeltas() ?? (0, 0, 0);
    }

    public (float normX, float normY, bool isTouching) GetTouchState()
    {
        return _touchReader?.GetTouchState() ?? (0, 0, false);
    }
    
    public static bool IsTouching(int regionX, int regionY, int regionWidth, int regionHeight)
    {
        if (Instance is null)
        {
            throw new Exception("Input not initialized.");
        }

        var (normalizedX, normalizedY, isTouching) = Instance.GetTouchState();

        if (!isTouching)
        {
            return false;
        }

        var x = normalizedY * _screenWidth;
        var y = normalizedX * _screenHeight;

        if (x >= regionX && x <= (regionX + regionWidth) &&
            y >= regionY && y <= (regionY + regionHeight))
        {
            return true;
        }
        
        return false;
    }

    public void Dispose()
    {
        _keyReader?.StopEventLoop();
        _mouseReader?.StopEventLoop();
        _touchReader?.StopEventLoop();
    }
}