using System.Xml.Serialization;

using MyPiUI.Drawing;

namespace MyPiUI.UI.Controls;

public class TextUIElement : UIElement
{
    [XmlAttribute("text")]
    public string? Text { get; set; }
    
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
}