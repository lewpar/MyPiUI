namespace MyKUIPi.Input;

public class InputManager : IDisposable
{
    public static InputManager? Instance { get; set; }
    
    private static int _screenWidth;
    private static int _screenHeight;
    
    private TouchReader? _touchReader;

    public InputManager(MyEngineOptions options)
    {
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
        
        _touchReader?.Initialize();
        _touchReader?.StartEventLoop();
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

        var x = normalizedX * _screenWidth;
        var y = normalizedY * _screenHeight;

        if (x >= regionX && x <= (regionX + regionWidth) &&
            y >= regionY && y <= (regionY + regionHeight))
        {
            return true;
        }
        
        return false;
    }

    public void Dispose()
    {
        _touchReader?.StopEventLoop();
    }
}