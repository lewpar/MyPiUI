using System.Xml.Serialization;
using MyPiUI.Primitives;
using MyPiUI.Drawing;

namespace MyPiUI.UI.Controls;

public class RectangleElement : UIElement
{
    public override void Draw(DrawBuffer buffer)
    {
        if (Background is not null)
        {
            buffer.FillRect(X, Y, Width, Height, Background.Value);   
        }
    }
}