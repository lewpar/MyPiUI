using System.Xml.Serialization;

using MyKUIPi.Drawing;
using MyKUIPi.Primitives;

namespace MyKUIPi.UI.Controls;

public class RectangleElement : UIElement
{
    public override void Draw(FrameBuffer buffer)
    {
        buffer.FillRect(X, Y, Width, Height, Background);
    }
}