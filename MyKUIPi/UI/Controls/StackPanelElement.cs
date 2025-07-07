using System.Xml.Serialization;
using MyKUIPi.Drawing;
using MyKUIPi.Primitives;

namespace MyKUIPi.UI.Controls;

public class StackPanelElement : UIElement
{
    [XmlAttribute("orientation")]
    public int Orientation { get; set; }
    
    [XmlAttribute("gap")]
    public int Gap { get; set; }

    public override void Init()
    {
        int offsetX = X + Padding;
        int offsetY = Y + Padding;

        foreach (var child in Children)
        {
            child.X = offsetX;
            child.Y = offsetY;

            if (Orientation == (int)StackOrientation.Vertical)
            {
                offsetY += child.Height + Gap;
                child.Width = Width - Padding * 2;
            }
            else
            {
                offsetX += child.Width + Gap;
                child.Height = Height - Padding * 2;
            }
            
            child.Init();
        }
    }

    public override void Draw(DrawBuffer buffer)
    {
        int offsetX = X + Padding;
        int offsetY = Y + Padding;
        
        var panelBounds = new Rectangle(X, Y, Width, Height);

        buffer.SetClipRect(panelBounds);

        foreach (var child in Children)
        {
            child.X = offsetX;
            child.Y = offsetY;

            if (Orientation == (int)StackOrientation.Vertical)
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

        buffer.ClearClipRect();
    }
}