using System.Xml.Serialization;

using MyKUIPi.Drawing;
using MyKUIPi.Primitives;

namespace MyKUIPi.UI.Controls;

public class RectangleElement : UIElement
{
    public override void Draw(DrawBuffer buffer)
    {
        buffer.FillRect(X, Y, Width, Height, Color.White);
    }
}