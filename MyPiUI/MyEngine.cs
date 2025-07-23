using System.Diagnostics;

using MyPiUI.Configuration;
using MyPiUI.Drawing;
using MyPiUI.Drawing.RenderTargets;
using MyPiUI.Input;
using MyPiUI.Primitives;
using MyPiUI.Scene;
using MyPiUI.UI.Controls;

namespace MyPiUI;

public class MyEngine : IDisposable
{
    public static MyEngine? Instance;
    
    public MyEngineOptions MyOptions { get => _myOptions; }
    private MyEngineOptions _myOptions;

    public SceneManager SceneManager { get => _sceneManager; }
    private SceneManager _sceneManager;

    public InputManager InputManager { get => _inputManager; }
    private InputManager _inputManager;

    public MyGraphicsContext GraphicsContext
    {
        get;
        init;
    }
    
    private IRenderTarget _renderTarget;
    private DrawBuffer _drawBuffer;

    private long _deltaTimeMs;
    private Stopwatch _deltaTimer;

    private Vector2 _touchCursorPosition;

    private RenderTimingMetrics? _drawMetrics;

    private bool _isCalibratingTouch;

    public MyEngine(MyEngineOptions myOptions)
    {
        _myOptions = myOptions;
        _sceneManager = new SceneManager(myOptions);
        _inputManager = new InputManager(myOptions);
        _deltaTimer = new Stopwatch();
        _touchCursorPosition = new Vector2(0, 0);

        if (Instance is null)
        {
            Instance = this;
        }

        switch (myOptions.RenderMode)
        {
            case RenderMode.FrameBuffer:
                _renderTarget = new FrameBufferRenderTarget(myOptions.FrameBufferDevice);
                break;
            
            case RenderMode.Raylib:
                _renderTarget = new RaylibRenderTarget(myOptions.RenderWidth, myOptions.RenderHeight);
                break;
            
            default:
                throw new Exception("Invalid render mode, no render target could be selected.");
        }

        GraphicsContext = _renderTarget.CreateGraphicsContext();
        _drawBuffer = new DrawBuffer(GraphicsContext);
        _drawBuffer.SetClearColor(myOptions.BackgroundColor);
        _drawBuffer.Clear();
    }

    public void Initialize()
    {
        RuntimeConfig.Load();
        
        if (_myOptions.RenderMode == RenderMode.FrameBuffer)
        {
            if (!File.Exists(MyOptions.FrameBufferDevice))
            {
                throw new Exception($"Failed to find frame buffer at path '{MyOptions.FrameBufferDevice}'.");
            }    
        }

        if (!File.Exists(MyOptions.TouchDevice))
        {
            throw new Exception($"Failed to find touch input device at path '{MyOptions.TouchDevice}'.");
        }

        if (_myOptions.RenderMode == RenderMode.FrameBuffer &&
            _myOptions.HideConsoleCaret)
        {
            Console.Write("\x1b[?25l");
        }
        
        _inputManager.Initialize(_myOptions.RenderWidth, _myOptions.RenderHeight);
        
        if (!_myOptions.SkipTouchCalibration)
        {
            var config = RuntimeConfig.Instance;
            if (config is null)
            {
                throw new Exception("Failed to start touch calibration, runtime config is not loaded.");
            }
            
            if (config is { MinTouchX: 0, MinTouchY: 0, MaxTouchX: 0, MaxTouchY: 0 })
            {
                CalibrateTouch();
            }
        }
    }
    
    private Point MeasureText(string text, int fontSize)
    {
        var textWidth = text.Length * fontSize;
        var textHeight = fontSize;
        
        return new Point(textWidth, textHeight);
    }

    private void CalibrateTouch(int holdTime = 3500)
    {
        if (_renderTarget is null)
        {
            throw new Exception("No render target initialized.");
        }

        _isCalibratingTouch = true;
        bool topLeftComplete = false;
        bool pointConfirmed = false;

        var targetHoldTimeMs = holdTime;
        var holdStartTime = DateTime.MinValue;

        Point? topLeft = null;
        Point? bottomRight = null;

        while (_isCalibratingTouch)
        {
            _drawBuffer.ClearDirtyRegions();

            var (x, y, isTouching) = _inputManager.GetAbsTouchState();
            double heldDuration = 0;

            if (isTouching)
            {
                if (holdStartTime == DateTime.MinValue)
                    holdStartTime = DateTime.UtcNow;

                heldDuration = (DateTime.UtcNow - holdStartTime).TotalMilliseconds;

                if (heldDuration >= targetHoldTimeMs && !pointConfirmed)
                {
                    if (!topLeftComplete)
                    {
                        topLeft = new Point((int)x, (int)y);
                        topLeftComplete = true;
                    }
                    else
                    {
                        bottomRight = new Point((int)x, (int)y);
                        _isCalibratingTouch = false;
                    }

                    holdStartTime = DateTime.MinValue;
                    pointConfirmed = false;
                    continue;
                }
            }
            else
            {
                holdStartTime = DateTime.MinValue;
                pointConfirmed = false;
            }

            // Draw main calibration instruction text
            var fontSize = 25;
            var phase = topLeftComplete ? "Bottom Right" : "Top Left";
            var mainText = $"Touch {phase} - {x:F0}, {y:F0}";
            var mainSize = MeasureText(mainText, fontSize);
            var mainX = (_myOptions.RenderWidth / 2) - (mainSize.X / 2);
            var mainY = (_myOptions.RenderHeight / 2) - (mainSize.Y / 2);

            _drawBuffer.DrawText(mainX, mainY, mainText, fontSize, Color.White);

            // Draw hold progress text (if touching)
            if (isTouching)
            {
                var holdText = $"Hold: {Math.Min(heldDuration, targetHoldTimeMs):F0} / {targetHoldTimeMs} ms";
                var holdSize = MeasureText(holdText, fontSize);
                var holdX = (_myOptions.RenderWidth / 2) - (holdSize.X / 2);
                var holdY = mainY + mainSize.Y + 10; // 10 pixels below main text

                _drawBuffer.DrawText(holdX, holdY, holdText, fontSize, Color.White);
            }
            
            _renderTarget.SwapBuffer(_drawBuffer.GetBuffer());
        }

        if (topLeft is null || bottomRight is null)
        {
            throw new Exception("Failed to calibrate touch input.");
        }

        var config = RuntimeConfig.Instance;
        if (config is null)
        {
            throw new Exception("Failed to save touch calibration, runtime config is not loaded.");
        }

        config.MinTouchX = Math.Min(topLeft.X, bottomRight.X);
        config.MinTouchY = Math.Min(topLeft.Y, bottomRight.Y);
        config.MaxTouchX = Math.Max(topLeft.X, bottomRight.X);
        config.MaxTouchY = Math.Max(topLeft.Y, bottomRight.Y);
        
        config.Save();
    }

    private void RenderMetrics()
    {
        if (_drawMetrics is null)
        {
            return;
        }
        
        int x = 15, y = 15, lineHeight = 12;
        int totalHeight = lineHeight * 8 + lineHeight;
        int totalWidth = 8 * 20;
        int fontSize = 12;
        var color = _myOptions.ForegroundColor;

        _drawBuffer.FillRect(0, 0, totalWidth, totalHeight, _myOptions.BackgroundColor);
        _drawBuffer.DrawText(x, y, $"Frame Δ: {_deltaTimeMs} ms", fontSize, color); y += lineHeight;
        _drawBuffer.DrawText(x, y, $"Clear: {_drawMetrics.ClearTime:F2} ms", fontSize, color); y += lineHeight;
        _drawBuffer.DrawText(x, y, $"Scene: {_drawMetrics.SceneDrawTime:F2} ms", fontSize, color); y += lineHeight;
        _drawBuffer.DrawText(x, y, $"UI: {_drawMetrics.UIDrawTime:F2} ms", fontSize, color); y += lineHeight;
        _drawBuffer.DrawText(x, y, $"Debug UI: {_drawMetrics.DebugUIDrawTime:F2} ms", fontSize, color); y += lineHeight;
        _drawBuffer.DrawText(x, y, $"Metrics: {_drawMetrics.MetricsTime:F2} ms", fontSize, color); y += lineHeight;
        _drawBuffer.DrawText(x, y, $"Swap: {_drawMetrics.SwapTime:F2} ms", fontSize, color); y += lineHeight;
        _drawBuffer.DrawText(x, y, $"Total: {_drawMetrics.TotalDrawTime:F2} ms", fontSize, color);
    }

    private void UpdateTouchPosition()
    {
        var (x, y, _) = _inputManager.GetTouchState();
        _touchCursorPosition = new Vector2(x, y);
    }

    private void RenderTouchCursor()
    {
        var (_, _, isTouching) = _inputManager.GetTouchState();

        var x = _touchCursorPosition.X * _myOptions.RenderWidth;
        var y = _touchCursorPosition.Y * _myOptions.RenderHeight;

        _drawBuffer.FillRect((int)x, (int)y, 10, 10, isTouching ? Color.Red : Color.Gray);
    }

    private void RenderDebugUI(UIElement element)
    {
        var fontSize = 12;
        _drawBuffer.DrawRect(element.X, element.Y, element.Width, element.Height, 1, Color.Red);
        _drawBuffer.DrawText(element.X, element.Y - fontSize - 1, element.GetType().Name, fontSize, Color.White);
        
        foreach (var child in element.Children)
        {
            RenderDebugUI(child);
        }
    }

    public void Update()
    {
        if (_drawBuffer is null)
            throw new Exception("Frame buffer not initialized.");

        if (SceneManager.CurrentScene is null)
            throw new Exception("No scene available to render.");

        if (SceneManager.CurrentScene.Input is null)
            SceneManager.CurrentScene.Input = _inputManager;

#if !DEBUG
    if (Console.KeyAvailable)
        _ = Console.ReadKey(true);
#endif

        SceneManager.CurrentScene.Update(_deltaTimeMs);
        SceneManager.CurrentScene.UIFrame?.Update(_deltaTimeMs);

        if (!string.IsNullOrWhiteSpace(_myOptions.TouchDevice))
        {
            UpdateTouchPosition();
        }
    }

    public void Draw()
    {
        if (_drawBuffer is null)
        {
            throw new Exception("Frame buffer not initialized.");
        }

        if (SceneManager.CurrentScene is null)
        {
            throw new Exception("No scene available to render.");
        }

        _deltaTimeMs = _deltaTimer.ElapsedMilliseconds;
        _deltaTimer.Restart();

        var totalTimer = Stopwatch.StartNew();

        // Clear Dirty Regions
        var clearTimer = Stopwatch.StartNew();
        _drawBuffer.ClearDirtyRegions();
        clearTimer.Stop();

        // Scene Draw
        var sceneDrawTimer = Stopwatch.StartNew();
        SceneManager.CurrentScene.Draw(_drawBuffer);
        sceneDrawTimer.Stop();

        // UI Draw
        var uiDrawTimer = Stopwatch.StartNew();
        SceneManager.CurrentScene.UIFrame?.Draw(_drawBuffer);
        uiDrawTimer.Stop();

        // Debug UI Draw
        var debugUIDrawTimer = Stopwatch.StartNew();
        if (SceneManager.CurrentScene.UIFrame is not null &&
            _myOptions.ShowDebugUI)
        {
            RenderDebugUI(SceneManager.CurrentScene.UIFrame);
        }
        debugUIDrawTimer.Stop();

        // Metrics Render
        var metricsTimer = Stopwatch.StartNew();
        if (_myOptions.ShowMetrics)
        {
            RenderMetrics();
        }
        metricsTimer.Stop();

        // Touch Cursor
        if (_myOptions.ShowDebugUI && 
            !string.IsNullOrWhiteSpace(_myOptions.TouchDevice))
        {
            RenderTouchCursor();
        }

        // Swap Buffers
        var swapTimer = Stopwatch.StartNew();
        _renderTarget.SwapBuffer(_drawBuffer.GetBuffer());
        swapTimer.Stop();

        totalTimer.Stop();

        _drawMetrics = new RenderTimingMetrics
        {
            ClearTime = clearTimer.Elapsed.TotalMilliseconds,
            SceneDrawTime = sceneDrawTimer.Elapsed.TotalMilliseconds,
            UIDrawTime = uiDrawTimer.Elapsed.TotalMilliseconds,
            DebugUIDrawTime = debugUIDrawTimer.Elapsed.TotalMilliseconds,
            MetricsTime = metricsTimer.Elapsed.TotalMilliseconds,
            SwapTime = swapTimer.Elapsed.TotalMilliseconds,
            TotalDrawTime = totalTimer.Elapsed.TotalMilliseconds,
        };
    }
    
    public void Dispose()
    {
        _inputManager.Dispose();
    }
}