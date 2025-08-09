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
            buffer.DrawText(new Point(X, Y), Text, FontFamily, FontSize, Foreground);
        }
    }
}