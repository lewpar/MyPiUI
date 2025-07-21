using System.Diagnostics;
using System.Xml.Serialization;

using MyPiUI.Drawing;
using MyPiUI.Primitives;

namespace MyPiUI.UI.Controls;

public class ImageElement : UIElement
{
    [XmlAttribute("source")]
    public string? Source { get; set; }
    
    [XmlIgnore]
    public BitmapImage? Image { get; private set; }
    
    private MyGraphicsContext? _graphicsContext;

    public override void Init(MyGraphicsContext graphicsContext)
    {
        _graphicsContext = graphicsContext;
    }

    public void LoadImage()
    {
        Debug.Assert(_graphicsContext is not null);
        
        if (string.IsNullOrWhiteSpace(Source))
        {
            throw new Exception("Source cannot be null or empty.");
        }

        if (!File.Exists(Source))
        {
            throw new FileNotFoundException("Source file not found.", Source);
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
        
        Image = BitmapImage.Load(Source, _graphicsContext.BitsPerPixel, width, height);
        
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