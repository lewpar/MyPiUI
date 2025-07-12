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

        int? bitsPerPixel = null;

        switch (MyEngine.Instance?.MyOptions.PixelFormat)
        {
            case MyPixelFormat.R8G8B8A8:
            case MyPixelFormat.B8G8R8A8:
                bitsPerPixel = 32;
                break;
            
            case MyPixelFormat.R5G6B5:
                bitsPerPixel = 16;
                break;
            
            default:
                throw new Exception("Unsupported pixel format.");
        }

        var width = Width;
        var height = Height;

        if (width <= 0 &&
            Parent is not null)
        {
            width = Parent.Width;
        }

        if (height <= 0 &&
            Parent is not null)
        {
            height = Parent.Height;
        }
        
        Image = BitmapImage.Load(Source, bitsPerPixel.Value, width, height);
        
        Width = Image.Width;
        Height = Image.Height;
    }

    public override void Draw(DrawBuffer buffer)
    {
        if (Image is null)
        {
            return;
        }
        
        buffer.SetClipRect(new Rectangle(X, Y, Width, Height));
        buffer.DrawImage(X, Y, Image);
        buffer.ClearClipRect();
    }
}