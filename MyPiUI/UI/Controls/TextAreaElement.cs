using MyPiUI.Drawing;
using MyPiUI.Drawing.Buffers;
using MyPiUI.Primitives;

namespace MyPiUI.UI.Controls;

public class TextAreaElement : TextUIElement
{
    private IDrawBuffer? _buffer;

    public override void Init(MyGraphicsContext graphicsContext, IDrawBuffer buffer)
    {
        _buffer = buffer;
        CalculateBounds();
        
        base.Init(graphicsContext, buffer);
    }

    public override void Draw(IDrawBuffer buffer)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(Text) &&
            !string.IsNullOrWhiteSpace(FontFamily))
        {
            var size = buffer.MeasureText(Text, FontFamily, FontSize);
            buffer.DrawText(new Point(X, Y + size.Height), Text, FontFamily, FontSize, Foreground);
        }
    }
    
    public override void CalculateBounds()
    {
        if (string.IsNullOrWhiteSpace(Text) ||
            string.IsNullOrWhiteSpace(FontFamily) ||
            _buffer is null)
        {
            return;
        }

        var size = _buffer.MeasureText(Text, FontFamily, FontSize);
        
        Width = size.Width;
        Height = size.Height;
    }
}