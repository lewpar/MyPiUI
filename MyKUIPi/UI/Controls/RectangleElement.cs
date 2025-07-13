using System.Xml.Serialization;

using MyKUIPi.Drawing;
using MyKUIPi.Primitives;

namespace MyKUIPi.UI.Controls;

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