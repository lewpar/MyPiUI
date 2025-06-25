namespace MyKUIPi.Drawing;

public class FrameBufferInfo
{
    public string? Mode { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int VirtualWidth { get; set; }
    public int VirtualHeight { get; set; }
    public int Depth { get; set; }

    public int[] Timings { get; set; } = new int[7];

    public RgbaInfo? Rgba { get; set; }

    public class RgbaInfo
    {
        public int RedLength { get; set; }
        public int RedOffset { get; set; }

        public int GreenLength { get; set; }
        public int GreenOffset { get; set; }

        public int BlueLength { get; set; }
        public int BlueOffset { get; set; }

        public int AlphaLength { get; set; }
        public int AlphaOffset { get; set; }
    }
}