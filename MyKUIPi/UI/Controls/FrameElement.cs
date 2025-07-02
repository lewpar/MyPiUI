using System.Xml.Serialization;
using MyKUIPi.Drawing;

namespace MyKUIPi.UI.Controls;

[XmlRoot("Frame", Namespace = UIHandler.Namespace)]
public class FrameElement : UIElement
{
    public override void Init()
    {
        if (Children.Count < 1)
        {
            return;
        }

        if (Height == 0)
        {
            Height = MyEngine.Instance?.FrameBufferInfo?.Height ?? 0;
        }

        if (Width == 0)
        {
            Width = MyEngine.Instance?.FrameBufferInfo?.Width ?? 0;
        }

        var child = Children[0];
        
        if (child.Width != Width)
        {
            child.X = Padding != 0 ? Padding : 0;
            child.Width = Padding != 0 ? Width - (Padding * 2) : Width;
        }

        if (child.Height != Height)
        {
            child.Y = Padding != 0 ? Padding : 0;
            child.Height = Padding != 0 ? Height - (Padding * 2) : Height;
        }
        
        child.Init();
    }

    public override void Draw(FrameBuffer buffer)
    {
        if (Children.Count < 1)
        {
            return;
        }

        var child = Children[0];
        
        child.Draw(buffer);
    }
}