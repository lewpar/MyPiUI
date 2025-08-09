using MyPiUI.Primitives;

using SkiaSharp;

namespace MyPiUI.Drawing.Buffers;

public class SkiaDrawBuffer : IDrawBuffer
{
    private readonly SKSurface _surface;
    private readonly SKCanvas _canvas;

    private readonly Dictionary<Color, SKColor> _cachedColors;
    private readonly Dictionary<(string, float), SKFont> _cachedFonts;
    private Color _clearColor;
    
    public SkiaDrawBuffer(MyGraphicsContext context)
    {
        var skColorType = GetSkColorType(context.PixelFormat);
        
        _surface = SKSurface.Create(new SKImageInfo()
        {
            AlphaType = SKAlphaType.Opaque,
            
            ColorType = skColorType,
            ColorSpace = SKColorSpace.CreateSrgb(),
            
            Width = context.Width,
            Height = context.Height
        });

        _canvas = _surface.Canvas;

        _cachedColors = new Dictionary<Color, SKColor>();
        _cachedFonts = new Dictionary<(string, float), SKFont>();
        _clearColor = Color.Black;
    }

    private SKFont GetSkFont(string fontFamily, float fontSize)
    {
        if (_cachedFonts.TryGetValue((fontFamily, fontSize), out var font))
        {
            return font;
        }
        
        _cachedFonts.Add((fontFamily, fontSize), new SKFont(SKTypeface.FromFamilyName(fontFamily), fontSize));

        return _cachedFonts[(fontFamily, fontSize)];
    }

    private SKColorType GetSkColorType(MyPixelFormat pixelFormat)
    {
        switch (pixelFormat)
        {
            case MyPixelFormat.R5G6B5:
                return SKColorType.Rgb565;
            
            case MyPixelFormat.R8G8B8A8:
                return SKColorType.Rgba8888;
            
            case MyPixelFormat.B8G8R8A8:
                return SKColorType.Bgra8888;
        }

        throw new Exception($"Invalid pixel format '{pixelFormat}'. This pixel format is not supported.");
    }

    private SKColor GetSkColor(Color color)
    {
        if (_cachedColors.TryGetValue(color, out var skColor))
        {
            return skColor;
        }
        
        _cachedColors.Add(color, new SKColor(color.R, color.G, color.B));

        return _cachedColors[color];
    }

    public void SetClearColor(Color color)
    {
        _clearColor = color;
    }

    public void Clear()
    {
        _canvas.Clear(GetSkColor(_clearColor));
    }

    public void Clear(Color color)
    {
        _canvas.Clear(GetSkColor(color));
    }

    public void Clear(Rectangle region)
    {
        FillRect(region, _clearColor);
    }

    public void Clear(Color color, Rectangle region)
    {
        FillRect(region, color);
    }

    public void ClearDirtyRegions()
    {
        
    }

    public void SetClipRect(Rectangle region)
    {
        _canvas.ClipRect(new SKRect(region.X, region.Y, 
            region.X + region.Width, region.Y + region.Height));
    }

    public void ClearClipRect()
    {
        _canvas.Restore();
    }

    public void DrawLine(Point a, Point b, Color color)
    {
        _canvas.DrawLine(a.X, a.Y, b.X, b.Y, new SKPaint()
        {
            Color = GetSkColor(color)
        });
    }

    public void DrawRect(Rectangle rect, Color color)
    {
        _canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, new SKPaint()
        {
            Color = GetSkColor(color),
            Style = SKPaintStyle.Stroke
        });
    }

    public void FillRect(Rectangle rect, Color color)
    {
        _canvas.DrawRect(rect.X, rect.Y, rect.Width, rect.Height, new SKPaint()
        {
            Color = GetSkColor(color),
            Style = SKPaintStyle.Fill
        });
    }

    public void DrawImage(Rectangle rect, Span<byte> image)
    {
        var skImage = SKImage.FromPixelCopy(new SKImageInfo()
        {
            ColorType = SKColorType.Bgra8888,
            AlphaType = SKAlphaType.Unpremul,
            
            Width = rect.Width,
            Height = rect.Height
        }, image);

        _canvas.DrawImage(skImage, new SKPoint(rect.X, rect.Y));
    }

    public void DrawText(Point position, string text, string fontFamily, float fontSize, Color color)
    {
        _canvas.DrawText(text, position.X, position.Y, 
            GetSkFont(fontFamily, fontSize),
            new SKPaint()
            {
                Color = GetSkColor(color)
            });
    }

    public Size MeasureText(string text, string fontFamily, float fontSize)
    {
        var font = GetSkFont(fontFamily, fontSize);
        font.MeasureText(text, out var skRect);

        return new Size()
        {
            Width = (int)skRect.Width,
            Height = (int)skRect.Height
        };
    }

    public Span<byte> GetBuffer()
    {
        return _surface.Snapshot().PeekPixels().GetPixelSpan();
    }
}