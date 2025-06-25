namespace MyKUIPi;

public class MyEngineOptions
{
    public required string FrameBufferDevice { get; init; }
    public string? KeyboardDevice { get; init; }
    public string? MouseDevice { get; init; }
    public string? TouchDevice { get; init; }
    public int MaxTouchX { get; init; } = 4096;
    public int MaxTouchY { get; init; } = 4096;
    public bool ShowMetrics { get; init; }
    public bool ShowDebugUI { get; init; }
    public bool HideConsoleCaret { get; init; }
}