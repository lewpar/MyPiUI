using MyPiUI.Drawing;
using MyPiUI.Drawing.Buffers;
using MyPiUI.Primitives;

namespace MyPiUI.UI.Controls;

public class TextAreaElement : TextUIElement
{
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
}