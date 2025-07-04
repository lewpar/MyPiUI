using System.Xml.Serialization;
using MyKUIPi.Drawing;

namespace MyKUIPi.UI.Controls;

public class TextAreaElement : UIElement
{
    [XmlAttribute("text")]
    public string? Text { get; set; }
    
    [XmlAttribute("font-size")]
    public int FontSize { get; set; }

    public override void Draw(FrameBuffer buffer)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return;
        }
        
        buffer.DrawText(X, Y, Text, Foreground, FontSize);
    }
}