using MyPiUI.Primitives;

namespace MyPiUI.Drawing.Buffers;

public interface IDrawBuffer
{
    public void SetClearColor(Color color);
    public void Clear();
    public void Clear(Color color);
    public void Clear(Rectangle region);
    public void Clear(Color color, Rectangle region);

    public void SetClipRect(Rectangle region);
    public void ClearClipRect();

    public void DrawLine(Point a, Point b, Color color);
    
    public void DrawRect(Rectangle rect, Color color);
    public void FillRect(Rectangle rect, Color color);

    public void DrawImage(Rectangle rect, Span<byte> image);

    public void DrawText(Point position, string text, string fontFamily, float fontSize, Color color);
    public Size MeasureText(string text, string fontFamily, float fontSize);

    public Span<byte> GetBuffer();
}