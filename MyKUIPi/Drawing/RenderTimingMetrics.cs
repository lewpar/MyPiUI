namespace MyKUIPi.Drawing;

public class RenderTimingMetrics
{
    public double ClearTime { get; set; }
    public double SceneDrawTime { get; set; }
    public double UIDrawTime { get; set; }
    public double DebugUIDrawTime { get; set; }
    public double MetricsTime { get; set; }
    public double MouseTime { get; set; }
    public double TouchTime { get; set; }
    public double SwapTime { get; set; }
    public double TotalDrawTime { get; set; }
}
