using MyPiUI.Drawing;

namespace MyPiUI.UI.Controls;

public class TextAreaElement : TextUIElement
{
    public override void Draw(DrawBuffer buffer)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        if(!string.IsNullOrWhiteSpace(Text))
            buffer.DrawText(X, Y, Text, FontSize, Foreground);
    }
}