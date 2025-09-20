using MyPiUI.Drawing;
using MyPiUI.Primitives;

namespace MyPiUI;

public class MyEngineOptions
{
    public required string FrameBufferDevice { get; init; }
    public string? TouchDevice { get; init; }
    public bool SwapTouchXAndY { get; init; }
    public bool ShowMetrics { get; init; }
    public bool ShowDebugUI { get; init; }
    public bool HideConsoleCaret { get; init; }
    public Color BackgroundColor { get; init; } = Color.Black;
    public Color ForegroundColor { get; init; } = Color.White;
    public RenderMode RenderMode { get; init; } = RenderMode.FrameBuffer;

    public int RenderWidth { get; init; } = 1920;
    public int RenderHeight { get; init; } = 1080;
    public bool HotReload { get; init; }
    public bool SkipTouchCalibration { get; init; }
}