using System.Xml.Serialization;

using MyKUIPi.Drawing;
using MyKUIPi.Primitives;

namespace MyKUIPi.UI.Controls;

public abstract class UIElement
{
    [XmlElement("Grid", typeof(GridElement))]
    [XmlElement("Rectangle", typeof(RectangleElement))]
    [XmlElement("Button", typeof(ButtonElement))]
    [XmlElement("StackPanel", typeof(StackPanelElement))]
    public List<UIElement> Children { get; } = new List<UIElement>();
    
    [XmlIgnore]
    public UIElement? Parent { get; set; }
    
    [XmlAttribute("x")]
    public int X { get; set; }
    
    [XmlAttribute("y")]
    public int Y { get; set; }
    
    [XmlAttribute("width")]
    public int Width { get; set; }
    
    [XmlAttribute("height")]
    public int Height { get; set; }

    public virtual void Init()
    {
        foreach (var child in Children)
        {
            child.Init();
        }
    }

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