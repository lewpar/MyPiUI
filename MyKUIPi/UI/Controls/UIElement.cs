using System.Xml.Serialization;

using MyKUIPi.Drawing;
using MyKUIPi.Primitives;

namespace MyKUIPi.UI.Controls;

public abstract class UIElement
{
    [XmlElement("Grid", typeof(GridElement))]
    [XmlElement("Absolute", typeof(AbsoluteElement))]
    [XmlElement("Rectangle", typeof(RectangleElement))]
    [XmlElement("Button", typeof(ButtonElement))]
    [XmlElement("StackPanel", typeof(StackPanelElement))]
    [XmlElement("Image", typeof(ImageElement))]
    [XmlElement("TextArea", typeof(TextAreaElement))]
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
    
    [XmlAttribute("padding")]
    public int Padding { get; set; }
    
    public Color Foreground { get; set; }

    [XmlAttribute("foreground")]
    public string ForegroundHex
    {
        get => Color.ToHex(Foreground);
        set => Foreground = string.IsNullOrWhiteSpace(value) ? Color.White : Color.FromHex(value);
    }
    
    public Color Background { get; set; }

    [XmlAttribute("background")]
    public string BackgroundHex
    {
        get => Color.ToHex(Background);
        set => Background = string.IsNullOrWhiteSpace(value) ? Color.DodgerBlue : Color.FromHex(value);
    }

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