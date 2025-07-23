using System.Xml.Serialization;

using MyPiUI.Drawing;

namespace MyPiUI.UI.Controls;

public abstract class TextUIElement : UIElement
{
    private string? _text;
    [XmlAttribute("text")]
    public string? Text
    {
        get => _text;
        set
        {
            _text = value;
            CalculateBounds();
        }
    }
    
    [XmlAttribute("font-family")]
    public string? FontFamily { get; set; }

    private string? _bindableFontSize;
    [XmlAttribute("font-size")]
    public string? BindableFontSize
    {
        get => _bindableFontSize;
        set
        {
            _bindableFontSize = value;
            if (TryParseBindableInt(value, out var parsed))
                FontSize = parsed;
        }
    }

    [XmlIgnore]
    public int FontSize { get; set; }

    public override void Init(MyGraphicsContext graphicsContext)
    {
        CalculateBounds();
        base.Init(graphicsContext);
    }

    public void CalculateBounds()
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            return;
        }

        var (width, height) = FontRenderer.MeasureText(Text, FontSize);
        
        Width = width;
        Height = height;
    }
}