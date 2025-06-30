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

    private long _deltaTimeMs;
    private Stopwatch _deltaTimer;

    private Vector2 _mouseCursorPosition;
    private Vector2 _touchCursorPosition;

    public MyEngine(MyEngineOptions myOptions)
    {
        _myOptions = myOptions;
        _sceneManager = new SceneManager();
        _inputManager = new InputManager(myOptions);
        _deltaTimer = new Stopwatch();
        _mouseCursorPosition = new Vector2(0, 0);
        _touchCursorPosition = new Vector2(0, 0);
    }

    public void Initialize()
    {
        if (!File.Exists(MyOptions.FrameBufferDevice))
        {
            throw new Exception($"Failed to find frame buffer at path '{MyOptions.FrameBufferDevice}'.");
        }

        if (!File.Exists(MyOptions.KeyboardDevice))
        {
            throw new Exception($"Failed to find input device at path '{MyOptions.KeyboardDevice}'.");
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

        _mouseCursorPosition = new Vector2((int)(_frameBufferInfo.Width / 2), 
                                            (int)(_frameBufferInfo.Height / 2));
        
        _frameBuffer.Clear(_myOptions.BackgroundColor);
    }

    public static FrameBufferInfo? GetFrameBufferInfo()
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
        
        _frameBuffer.DrawText(15, 15, $"Frame Diff (ms): {_deltaTimeMs}", _myOptions.ForegroundColor);
    }

    private void UpdateMousePosition()
    {
        if (_frameBufferInfo is null)
        {
            return;
        }

        var (dx, dy, _) = _inputManager.GetMouseDelta();

        var newMouseX = _mouseCursorPosition.X + dx;
        var newMouseY = _mouseCursorPosition.Y + dy;

        if (newMouseX <= 0)
        {
            newMouseX = 0;
        }
        
        if (newMouseX >= _frameBufferInfo.Width)
        {
            newMouseX = _frameBufferInfo.Width;
        }

        if (newMouseY <= 0)
        {
            newMouseY = 0;
        }

        if (newMouseY >= _frameBufferInfo.Height)
        {
            newMouseY = _frameBufferInfo.Height;
        }

        _mouseCursorPosition = new Vector2(newMouseX, newMouseY);
    }

    private void RenderMouseCursor()
    {
        if (_frameBuffer is null)
        {
            return;
        }

        _frameBuffer.FillTriangle(new Vector2(_mouseCursorPosition.X, _mouseCursorPosition.Y),
                                    new Vector2(_mouseCursorPosition.X + 8, _mouseCursorPosition.Y + 5),
                                    new Vector2(_mouseCursorPosition.X + 2, _mouseCursorPosition.Y + 10),
                                    Color.Red);
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
        {
            throw new Exception("Frame buffer not initialized.");
        }

        if (SceneManager.CurrentScene is null)
        {
            throw new Exception("No scene available to render.");
        }

        if (SceneManager.CurrentScene.Input is null)
        {
            SceneManager.CurrentScene.Input = _inputManager;
        }

#if !DEBUG
        // Consume any console keys so they dont get rendered.
        if (Console.KeyAvailable)
        {
            _ = Console.ReadKey(true);
        }
#endif

        _deltaTimeMs = _deltaTimer.ElapsedMilliseconds;
        _deltaTimer.Restart();

        SceneManager.CurrentScene.Update(_deltaTimeMs);

        foreach (var dirtyRegion in _frameBuffer.DirtyRegions)
        {
            _frameBuffer.Clear(_myOptions.BackgroundColor, dirtyRegion);
        }
        _frameBuffer.DirtyRegions.Clear();
        
        SceneManager.CurrentScene.Draw(_frameBuffer);

        SceneManager.CurrentScene.UIFrame?.Update(_deltaTimeMs);
        SceneManager.CurrentScene.UIFrame?.Draw(_frameBuffer);
        
        if (SceneManager.CurrentScene.UIFrame is not null &&
            _myOptions.ShowDebugUI)
        {
            RenderDebugUI(SceneManager.CurrentScene.UIFrame);
        }

        if (_myOptions.ShowMetrics)
        {
            RenderMetrics();
        }

        if (!string.IsNullOrWhiteSpace(_myOptions.MouseDevice))
        {
            UpdateMousePosition();
            RenderMouseCursor();
        }

        if (!string.IsNullOrWhiteSpace(_myOptions.TouchDevice))
        {
            UpdateTouchPosition();
            RenderTouchCursor();
        }

        _frameBuffer.SwapBuffers();
    }

    public void Dispose()
    {
        _inputManager?.Dispose();
        _frameBuffer?.Dispose();
    }
}