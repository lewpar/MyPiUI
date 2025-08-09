using MyPiUI.Drawing.Buffers;

namespace MyPiUI.Drawing;

public class MyGraphicsContext
{
    public required int BitsPerPixel { get; init; }
    public required MyPixelFormat PixelFormat { get; init; }
    
    public required int Width { get; init; }
    public required int Height { get; init; }
}