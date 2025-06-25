using System.Xml.Serialization;

using MyKUIPi.Drawing;

namespace MyKUIPi.UI.Controls;

[XmlInclude(typeof(FrameElement))]
[XmlInclude(typeof(RectangleElement))]
[XmlInclude(typeof(ButtonElement))]
public abstract class UIElement
{
    [XmlElement("Grid", typeof(GridElement))]
    [XmlElement("Rectangle", typeof(RectangleElement))]
    [XmlElement("Button", typeof(ButtonElement))]
    public List<UIElement> Children { get; } = new List<UIElement>();
    
    [XmlIgnore]
    public UIElement? Parent { get; set; }

    public virtual void Draw(FrameBuffer buffer)
    {
        foreach (var child in Children)
        {
            child.Draw(buffer);
        }
    }

    public virtual void Update(float deltaTimeMs)
    {
        foreach (var child in Children)
        {
            child.Update(deltaTimeMs);
        }
    }

    public int MeasureText(int fontSize, string text)
    {
        return text.Length * fontSize;
    }
}