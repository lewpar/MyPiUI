using System.Xml.Serialization;
using MyPiUI.Drawing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;

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
            RecalculateBounds();
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
                RecalculateBounds();
            }
        }
    }

    private Font? _font;

    public override void Init(MyGraphicsContext graphicsContext)
    {
        var fontFamilies = SystemFonts.Families;
        var fontFamily = "Roboto Mono";
        
        FontFamily? targetFontFamily = fontFamilies.FirstOrDefault(f => f.Name == fontFamily);
        if (targetFontFamily is null)
        {
            throw new Exception($"Font '{fontFamily}' not found");   
        }

        _font = targetFontFamily.Value.CreateFont(FontSize);
        RecalculateBounds();
        
        base.Init(graphicsContext);
    }

    [XmlIgnore]
    public int FontSize { get; set; }

    private void RecalculateBounds()
    {
        if (_font is null || string.IsNullOrEmpty(Text))
        {
            return;
        }
        
        var textOptions = new RichTextOptions(_font)
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        
        var textSize = TextMeasurer.MeasureSize(Text, textOptions);
        Width = (int)Math.Ceiling(textSize.Width + 1);
        Height = (int)Math.Ceiling(_font.Size);
    }

    public override void Draw(DrawBuffer buffer)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        //buffer.DrawText(X, Y, Text, Foreground, FontSize);
        if(_font is not null)
            buffer.DrawTextNew(X, Y, Text, _font);
    }
}