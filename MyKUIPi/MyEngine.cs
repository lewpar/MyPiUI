using System.Diagnostics;
using System.Text.RegularExpressions;

using MyKUIPi.Drawing;
using MyKUIPi.Input;
using MyKUIPi.Primitives;
using MyKUIPi.Scene;
using MyKUIPi.UI.Controls;

namespace MyKUIPi;

public class MyEngine : IDisposable
{
    public static MyEngine? Instance;
    
    public MyEngineOptions MyOptions { get => _myOptions; }
    private MyEngineOptions _myOptions;

    public SceneManager SceneManager { get => _sceneManager; }
    private SceneManager _sceneManager;

    public InputManager InputManager { get => _inputManager; }
    private InputManager _inputManager;

    public FrameBuffer? FrameBuffer { get => _frameBuffer; }
    private FrameBuffer? _frameBuffer;

    public FrameBufferInfo? FrameBufferInfo { get => _frameBufferInfo; }
    private FrameBufferInfo? _frameBufferInfo;
    
    public int MaxTouchX { get; private set; }
    public int MaxTouchY { get; private set; }

    private long _deltaTimeMs;
    private Stopwatch _deltaTimer;

    private Vector2 _touchCursorPosition;

    private RenderTimingMetrics? _drawMetrics;

    private bool _isCalibratingTouch;

    public MyEngine(MyEngineOptions myOptions)
    {
        _myOptions = myOptions;
        _sceneManager = new SceneManager();
        _inputManager = new InputManager(myOptions);
        _deltaTimer = new Stopwatch();
        _touchCursorPosition = new Vector2(0, 0);

        if (Instance is null)
        {
            Instance = this;
        }
    }

    public void Initialize()
    {
        if (!File.Exists(MyOptions.FrameBufferDevice))
        {
            throw new Exception($"Failed to find frame buffer at path '{MyOptions.FrameBufferDevice}'.");
        }

        if (!File.Exists(MyOptions.TouchDevice))
        {
            throw new Exception($"Failed to find touch input device at path '{MyOptions.TouchDevice}'.");
        }

        var frameBufferInfo = GetFrameBufferInfo();
        if (frameBufferInfo is null)
        {
            throw new Exception("Failed to get frame buffer information from 'fbset'.");
        }

        if (frameBufferInfo.Depth != 16 && frameBufferInfo.Depth != 32)
        {
            throw new Exception($"Unsupported color depth: {frameBufferInfo.Depth}-bit. Only 16-bit and 32-bit are supported.");
        }

        _frameBufferInfo = frameBufferInfo;
        _frameBuffer = new FrameBuffer(frameBufferInfo, _myOptions);

        if (_myOptions.HideConsoleCaret)
        {
            Console.Write("\x1b[?25l");
        }

        _inputManager.Initialize(_frameBufferInfo.Width, _frameBufferInfo.Height);
        
        _frameBuffer.Clear(_myOptions.BackgroundColor);
    }
    
    private Point MeasureText(string text, int fontSize)
    {
        var textWidth = text.Length * fontSize;
        var textHeight = fontSize;
        
        return new Point(textWidth, textHeight);
    }

    public void CalibrateTouch()
    {
        if (_frameBuffer is null || _frameBufferInfo is null)
        {
            throw new Exception("Frame buffer not initialized. Ensure touch calibration is called after engine initialization and before render loop.");
        }

        _isCalibratingTouch = true;
        bool topLeftComplete = false;
        bool pointConfirmed = false;

        var targetHoldTimeMs = 5000;
        var holdStartTime = DateTime.MinValue;

        Point? topLeft = null;
        Point? bottomRight = null;

        while (_isCalibratingTouch)
        {
            foreach (var dirtyRegion in _frameBuffer.DirtyRegions)
            {
                _frameBuffer.Clear(_myOptions.BackgroundColor, dirtyRegion);
            }
            _frameBuffer.DirtyRegions.Clear();

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

            // Draw calibration instruction text
            var fontSize = 25;
            var phase = topLeftComplete ? "Bottom Right" : "Top Left";
            var mainText = $"Touch {phase} - {x:F0}, {y:F0}";
            var size = MeasureText(mainText, fontSize);
            _frameBuffer.DrawText((_frameBufferInfo.Width / 2) - (size.X / 2),
                                  (_frameBufferInfo.Height / 2) - (size.Y / 2),
                                  mainText, _myOptions.ForegroundColor, fontSize);

            // Show touch duration (if touching)
            if (isTouching)
            {
                var durationText = $"{Math.Min(heldDuration, targetHoldTimeMs):F0} ms";
                var durationSize = MeasureText(durationText, fontSize);
                _frameBuffer.DrawText((int)x - durationSize.X / 2,
                                      (int)y - 40, // above finger
                                      durationText, _myOptions.ForegroundColor, fontSize);
            }

            _frameBuffer.SwapBuffers();
        }

        if (topLeft is null || bottomRight is null)
        {
            throw new Exception("Failed to calibrate touch input.");
        }
        
        MaxTouchX = Math.Abs(topLeft.X - bottomRight.X);
        MaxTouchY = Math.Abs(topLeft.Y - bottomRight.Y);
    }
    
    private static FrameBufferInfo? GetFrameBufferInfo()
    {
        try
        {
            var startInfo = new ProcessStartInfo("fbset")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return ParseFrameBufferInfo(output);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get frame buffer information from 'fbset': {ex.Message}");
        }
    }

    private static FrameBufferInfo ParseFrameBufferInfo(string input)
    {
        var info = new FrameBufferInfo();

        var modeMatch = Regex.Match(input, @"mode\s+""(\d+x\d+)""");
        if (modeMatch.Success)
            info.Mode = modeMatch.Groups[1].Value;

        var geometryMatch = Regex.Match(input, @"geometry\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)");
        if (geometryMatch.Success)
        {
            info.Width = int.Parse(geometryMatch.Groups[1].Value);
            info.Height = int.Parse(geometryMatch.Groups[2].Value);
            info.VirtualWidth = int.Parse(geometryMatch.Groups[3].Value);
            info.VirtualHeight = int.Parse(geometryMatch.Groups[4].Value);
            info.Depth = int.Parse(geometryMatch.Groups[5].Value);
        }

        var timingsMatch = Regex.Match(input, @"timings\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)");
        if (timingsMatch.Success)
        {
            for (int i = 0; i < 7; i++)
            {
                info.Timings[i] = int.Parse(timingsMatch.Groups[i + 1].Value);
            }
        }

        var rgbaMatch = Regex.Match(input, @"rgba\s+(\d+)/(\d+),(\d+)/(\d+),(\d+)/(\d+),(\d+)/(\d+)");
        if (rgbaMatch.Success)
        {
            info.Rgba = new FrameBufferInfo.RgbaInfo
            {
                RedLength = int.Parse(rgbaMatch.Groups[1].Value),
                RedOffset = int.Parse(rgbaMatch.Groups[2].Value),

                GreenLength = int.Parse(rgbaMatch.Groups[3].Value),
                GreenOffset = int.Parse(rgbaMatch.Groups[4].Value),

                BlueLength = int.Parse(rgbaMatch.Groups[5].Value),
                BlueOffset = int.Parse(rgbaMatch.Groups[6].Value),

                AlphaLength = int.Parse(rgbaMatch.Groups[7].Value),
                AlphaOffset = int.Parse(rgbaMatch.Groups[8].Value),
            };
        }

        return info;
    }

    private void RenderMetrics()
    {
        if (_frameBuffer is null)
        {
            return;
        }

        if (_drawMetrics is null)
        {
            return;
        }
        
        int x = 15, y = 15, lineHeight = 12;
        int totalHeight = lineHeight * 9 + lineHeight;
        int totalWidth = 8 * 20;
        var color = _myOptions.ForegroundColor;

        _frameBuffer.FillRect(0, 0, totalWidth, totalHeight, _myOptions.BackgroundColor);
        _frameBuffer.DrawText(x, y, $"Frame Δ: {_deltaTimeMs} ms", color); y += lineHeight;
        _frameBuffer.DrawText(x, y, $"Clear: {_drawMetrics.ClearTime:F2} ms", color); y += lineHeight;
        _frameBuffer.DrawText(x, y, $"Scene: {_drawMetrics.SceneDrawTime:F2} ms", color); y += lineHeight;
        _frameBuffer.DrawText(x, y, $"UI: {_drawMetrics.UIDrawTime:F2} ms", color); y += lineHeight;
        _frameBuffer.DrawText(x, y, $"Debug UI: {_drawMetrics.DebugUIDrawTime:F2} ms", color); y += lineHeight;
        _frameBuffer.DrawText(x, y, $"Metrics: {_drawMetrics.MetricsTime:F2} ms", color); y += lineHeight;
        _frameBuffer.DrawText(x, y, $"Touch: {_drawMetrics.TouchTime:F2} ms", color); y += lineHeight;
        _frameBuffer.DrawText(x, y, $"Swap: {_drawMetrics.SwapTime:F2} ms", color); y += lineHeight;
        _frameBuffer.DrawText(x, y, $"Total: {_drawMetrics.TotalDrawTime:F2} ms", color);
    }

    private void UpdateTouchPosition()
    {
        var (x, y, _) = _inputManager.GetTouchState();
        _touchCursorPosition = new Vector2(x, y);
    }

    private void RenderTouchCursor()
    {
        if (_frameBuffer is null ||
            _frameBufferInfo is null)
        {
            return;
        }

        var (_, _, isTouching) = _inputManager.GetTouchState();

        var x = _touchCursorPosition.X * _frameBufferInfo.Width;
        var y = _touchCursorPosition.Y * _frameBufferInfo.Height;

        _frameBuffer.FillRect((int)x, (int)y, 10, 10, isTouching ? Color.Red : Color.Gray);
    }

    private void RenderDebugUI(UIElement element)
    {
        if (_frameBuffer is null)
        {
            return;
        }

        var fontSize = 8;
        _frameBuffer.DrawRect(element.X, element.Y, element.Width, element.Height, 1, Color.Red);
        _frameBuffer.DrawText(element.X, element.Y - fontSize - 1, element.GetType().Name, Color.White, fontSize);
        
        foreach (var child in element.Children)
        {
            RenderDebugUI(child);
        }
    }

    public void Update()
    {
        if (_frameBuffer is null)
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
        if (_frameBuffer is null)
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
        foreach (var dirtyRegion in _frameBuffer.DirtyRegions)
        {
            _frameBuffer.Clear(_myOptions.BackgroundColor, dirtyRegion);
        }
        _frameBuffer.DirtyRegions.Clear();
        clearTimer.Stop();

        // Scene Draw
        var sceneDrawTimer = Stopwatch.StartNew();
        SceneManager.CurrentScene.Draw(_frameBuffer);
        sceneDrawTimer.Stop();

        // UI Draw
        var uiDrawTimer = Stopwatch.StartNew();
        SceneManager.CurrentScene.UIFrame?.Draw(_frameBuffer);
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
        var touchTimer = Stopwatch.StartNew();
        if (!string.IsNullOrWhiteSpace(_myOptions.TouchDevice))
        {
            RenderTouchCursor();
        }
        touchTimer.Stop();

        // Swap Buffers
        var swapTimer = Stopwatch.StartNew();
        _frameBuffer.SwapBuffers();
        swapTimer.Stop();

        totalTimer.Stop();

        _drawMetrics = new RenderTimingMetrics
        {
            ClearTime = clearTimer.Elapsed.TotalMilliseconds,
            SceneDrawTime = sceneDrawTimer.Elapsed.TotalMilliseconds,
            UIDrawTime = uiDrawTimer.Elapsed.TotalMilliseconds,
            DebugUIDrawTime = debugUIDrawTimer.Elapsed.TotalMilliseconds,
            MetricsTime = metricsTimer.Elapsed.TotalMilliseconds,
            TouchTime = touchTimer.Elapsed.TotalMilliseconds,
            SwapTime = swapTimer.Elapsed.TotalMilliseconds,
            TotalDrawTime = totalTimer.Elapsed.TotalMilliseconds,
        };

    }
    
    public void Dispose()
    {
        _inputManager?.Dispose();
        _frameBuffer?.Dispose();
    }
}