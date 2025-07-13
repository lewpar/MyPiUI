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
    [XmlElement("Toggle", typeof(ToggleElement))]
    public List<UIElement> Children { get; } = new();

    [XmlIgnore]
    public UIElement? Parent { get; set; }

    private string? _bindableX;
    [XmlAttribute("x")]
    public string? BindableX
    {
        get => _bindableX;
        set
        {
            _bindableX = value;
            if (TryParseBindableInt(value, out var parsed))
                X = parsed;
        }
    }

    [XmlIgnore]
    public int X { get; set; }

    private string? _bindableY;
    [XmlAttribute("y")]
    public string? BindableY
    {
        get => _bindableY;
        set
        {
            _bindableY = value;
            if (TryParseBindableInt(value, out var parsed))
                Y = parsed;
        }
    }

    [XmlIgnore]
    public int Y { get; set; }

    private string? _bindableWidth;
    [XmlAttribute("width")]
    public string? BindableWidth
    {
        get => _bindableWidth;
        set
        {
            _bindableWidth = value;
            if (TryParseBindableInt(value, out var parsed))
                Width = parsed;
        }
    }

    [XmlIgnore]
    public int Width { get; set; }

    private string? _bindableHeight;
    [XmlAttribute("height")]
    public string? BindableHeight
    {
        get => _bindableHeight;
        set
        {
            _bindableHeight = value;
            if (TryParseBindableInt(value, out var parsed))
                Height = parsed;
        }
    }

    [XmlIgnore]
    public int Height { get; set; }

    private string? _bindablePadding;
    [XmlAttribute("padding")]
    public string? BindablePadding
    {
        get => _bindablePadding;
        set
        {
            _bindablePadding = value;
            if (TryParseBindableInt(value, out var parsed))
                Padding = parsed;
        }
    }

    [XmlIgnore]
    public int Padding { get; set; }

    public Color Foreground { get; set; }

    [XmlAttribute("foreground")]
    public string ForegroundHex
    {
        get => Color.ToHex(Foreground);
        set => Foreground = string.IsNullOrWhiteSpace(value) ? Color.White : Color.FromHex(value);
    }

    public Color? Background { get; set; }

    [XmlAttribute("background")]
    public string? BackgroundHex
    {
        get => Background is null ? null : Color.ToHex(Background.Value);
        set => Background = string.IsNullOrWhiteSpace(value) ? null : Color.FromHex(value);
    }

    public virtual void Init()
    {
        foreach (var child in Children)
        {
            child.Init();
        }
    }

    public virtual void Draw(DrawBuffer buffer)
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

    public bool TryParseBindableInt(string? input, out int result)
    {
        if (!string.IsNullOrWhiteSpace(input) && !input.StartsWith("{") && int.TryParse(input, out result))
            return true;

        result = 0;
        return false;
    }
}
