using System.Xml.Serialization;

using MyPiUI.Drawing;
using MyPiUI.Drawing.Buffers;

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

    private IDrawBuffer? _buffer;

    public override void Init(MyGraphicsContext graphicsContext, IDrawBuffer buffer)
    {
        _buffer = buffer;
        
        CalculateBounds();
        
        base.Init(graphicsContext, buffer);
    }

    public void CalculateBounds()
    {
        if (string.IsNullOrWhiteSpace(Text) ||
            string.IsNullOrWhiteSpace(FontFamily) ||
            _buffer is null)
        {
            return;
        }

        var size = _buffer.MeasureText(Text, FontFamily, FontSize);
        
        Width = size.Width;
        Height = size.Height;
    }
}