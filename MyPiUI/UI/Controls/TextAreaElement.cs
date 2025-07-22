using MyPiUI.Drawing;

namespace MyPiUI.UI.Controls;

public class TextAreaElement : TextUIElement
{
    public void CalculateBounds()
    {
        if (string.IsNullOrWhiteSpace(FontFamily) ||
            string.IsNullOrWhiteSpace(Text))
        {
            return;
        }

        var (width, height) = FontRenderer.MeasureText(Text, FontFamily, FontSize);
        
        Width = width;
        Height = height;
    }

    public override void Init(MyGraphicsContext graphicsContext)
    {
        CalculateBounds();
        base.Init(graphicsContext);
    }

    public override void Draw(DrawBuffer buffer)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        //buffer.DrawText(X, Y, Text, Foreground, FontSize);
        if(!string.IsNullOrWhiteSpace(FontFamily))
            buffer.DrawText(X, Y, Text, FontRenderer.GetOrCreateBitmapFont(FontFamily, FontSize));
    }
}