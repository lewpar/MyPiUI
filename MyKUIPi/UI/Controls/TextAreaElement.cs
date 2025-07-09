using System.Xml.Serialization;
using MyKUIPi.Drawing;

namespace MyKUIPi.UI.Controls;

public class TextAreaElement : UIElement
{
    private string? _bindableText;
    [XmlAttribute("text")]
    public string? Text
    {
        get => _bindableText;
        set => _bindableText = value;
    }

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

    public override void Draw(DrawBuffer buffer)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        buffer.DrawText(X, Y, Text, Foreground, FontSize);
    }
}