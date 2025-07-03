using MyKUIPi.Primitives;

namespace MyKUIPi;

public class MyEngineOptions
{
    public required string FrameBufferDevice { get; init; }
    public string? TouchDevice { get; init; }
    public bool ShowMetrics { get; init; }
    public bool ShowDebugUI { get; init; }
    public bool HideConsoleCaret { get; init; }
    public Color BackgroundColor { get; init; } = Color.Black;
    public Color ForegroundColor { get; init; } = Color.White;
}