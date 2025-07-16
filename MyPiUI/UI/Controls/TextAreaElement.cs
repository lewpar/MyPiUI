using System.Xml.Serialization;
using MyPiUI.Drawing;

namespace MyPiUI.UI.Controls;

public class TextAreaElement : UIElement
{
    private string? _bindableText;
    [XmlAttribute("text")]
    public string? Text
    {
        get => _bindableText;
        set
        {
            _bindableText = value;
            RecalculateBounds(value, FontSize);
        } 
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
            {
                FontSize = parsed;
                RecalculateBounds(Text, parsed);
            }
        }
    }

    [XmlIgnore]
    public int FontSize { get; set; }

    private void RecalculateBounds(string? text, int fontSize)
    {
        Width = MeasureText(fontSize, text ?? "");
        Height = FontSize;
    }

    public override void Draw(DrawBuffer buffer)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        buffer.DrawText(X, Y, Text, Foreground, FontSize);
    }
}