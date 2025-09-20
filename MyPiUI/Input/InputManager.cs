namespace MyPiUI.Input;

public class InputManager : IDisposable
{
    public static InputManager? Instance { get; set; }
    
    private static int _screenWidth;
    private static int _screenHeight;
    
    private readonly bool _isTouchXYSwapped;
    private readonly bool _isTouchXInverted;
    private readonly bool _isTouchYInverted;
    
    private TouchReader? _touchReader;

    public InputManager(MyEngineOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.TouchDevice))
        {
            _touchReader = new TouchReader(options.TouchDevice);
        }

        _isTouchXYSwapped = options.SwapTouchXAndY;
        _isTouchXInverted = options.InvertTouchX;
        _isTouchYInverted = options.InvertTouchY;

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
        if (_touchReader is null)
        {
            return (0, 0, false);
        }

        var (x, y, isTouching) = _touchReader.GetTouchState();
        
        if (_isTouchXYSwapped)
        {
            return (_isTouchXInverted ? -y : y, 
                _isTouchYInverted ? -x : x, isTouching);
        }
        
        return (_isTouchXInverted ? -x : x, 
            _isTouchYInverted ? -y : y, isTouching);
    }

    public (float x, float y, bool isTouching) GetAbsTouchState()
    {
        if (_touchReader is null)
        {
            return (0, 0, false);
        }

        var (x, y, isTouching) = _touchReader.GetAbsTouchState();
        
        if (_isTouchXYSwapped)
        {
            return (_isTouchXInverted ? -y : y, 
                _isTouchYInverted ? -x : x, isTouching);
        }
        
        return (_isTouchXInverted ? -x : x, 
            _isTouchYInverted ? -y : y, isTouching);
    }
    
    public static bool IsTouching()
    {
        if (Instance is null)
        {
            throw new Exception("Input not initialized.");
        }

        var (_, _, isTouching) = Instance.GetTouchState();
        
        return isTouching;
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