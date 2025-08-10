using MyPiUI.Drawing;
using MyPiUI.Drawing.Buffers;
using MyPiUI.Primitives;

namespace MyPiUI.UI.Controls;

public class RectangleElement : UIElement
{
    public override void Draw(IDrawBuffer buffer)
    {
        if (Background is not null)
        {
            buffer.FillRect(new Rectangle(X, Y, Width, Height), Background.Value);   
        }
    }
}