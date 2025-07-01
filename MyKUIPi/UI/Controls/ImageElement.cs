using System.Xml.Serialization;
using MyKUIPi.Drawing;
using MyKUIPi.Primitives;

namespace MyKUIPi.UI.Controls;

public class ImageElement : UIElement
{
    [XmlAttribute("source")]
    public string? Source { get; set; }
    
    [XmlIgnore]
    public BitmapImage? Image { get; private set; }

    public void LoadImage()
    {
        if (string.IsNullOrWhiteSpace(Source))
        {
            throw new Exception("Source cannot be null or empty.");
        }

        if (!File.Exists(Source))
        {
            throw new  FileNotFoundException("Source file not found.", Source);
        }
        
        // Load image from disk and bit-depth of frame buffer. Default to 16-bit color if a failure occurs.
        Image = BitmapImage.Load(Source, MyEngine.Instance is null || MyEngine.Instance.FrameBufferInfo is null ? 
                                            16 : 
                                            MyEngine.Instance.FrameBufferInfo.Depth);
        
        Width = Image.Width;
        Height = Image.Height;
    }

    public override void Draw(FrameBuffer buffer)
    {
        if (Image is null)
        {
            return;
        }
        
        buffer.SetClip(new Rectangle(X, Y, Width, Height));
        buffer.DrawImage(X, Y, Image);
        buffer.ClearClip();
    }
}