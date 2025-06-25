using System.Xml.Serialization;
using MyKUIPi.Drawing;
using MyKUIPi.Primitives;

namespace MyKUIPi.UI.Controls;

public class StackPanelElement : UIElement
{
    private StackOrientation _orientation;

    [XmlAttribute("orientation")]
    public int Orientation
    {
        set => _orientation = (StackOrientation)value;
    }
    
    [XmlAttribute("padding")]
    public int Padding { get; set; }
    
    [XmlAttribute("gap")]
    public int Gap { get; set; }
    
    public override void Draw(FrameBuffer buffer)
    {
        int offsetX = X + Padding;
        int offsetY = Y + Padding;

        var panelBounds = new Rectangle(X, Y, Width, Height);

        buffer.SetClip(panelBounds);

        foreach (var child in Children)
        {
            child.X = offsetX;
            child.Y = offsetY;

            if (_orientation == StackOrientation.Vertical)
            {
                offsetY += child.Height + Gap;
                child.Width = Width - Padding * 2;
            }
            else
            {
                offsetX += child.Width + Gap;
                child.Height = Height - Padding * 2;
            }

            child.Draw(buffer);
        }

        buffer.ClearClip();
    }
}