using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MyPiUI.Drawing;

public class FontGlyph
{
    public Rectangle AtlasBounds;
    public int OffsetX;
    public int OffsetY;
    public int AdvanceX;
}

public class BitmapFont
{
    public required Image<Rgba32> Atlas;
    public required Dictionary<char, FontGlyph> Glyphs;
    public int LineHeight;
    public required Font Font;
}

public class FontRenderer
{
    private static readonly Dictionary<string, BitmapFont> FontCache = new();
    
    public static Font LoadFont(string fontFamily, int fontSize = 14)
    {
        var fontFamilies = SystemFonts.Families;
        
        var targetFontFamily = fontFamilies.FirstOrDefault(f => f.Name == fontFamily);
        if (targetFontFamily == default(FontFamily))
        {
            throw new Exception($"Font '{fontFamily}' not found.");
        }

        var font = targetFontFamily.CreateFont(fontSize);
        return font;
    }
    
    public static BitmapFont GetOrCreateBitmapFont(string fontFamily, int fontSize)
    {
        string key = $"{fontFamily}:{fontSize}";
        if (FontCache.TryGetValue(key, out var cached))
            return cached;

        var bitmapFont = CacheFontGlyphs(fontFamily, fontSize);
        FontCache[key] = bitmapFont;
        
        return bitmapFont;
    }

    public static BitmapFont CacheFontGlyphs(string fontFamily, int fontSize)
    {
        Font font = LoadFont(fontFamily, fontSize);
        RichTextOptions options = new(font)
        {
            Dpi = 72,
            Origin = PointF.Empty,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        var charset = Enumerable.Range(32, 95).Select(i => (char)i); // Printable ASCII

        var glyphs = new Dictionary<char, FontGlyph>();
        var glyphImages = new List<(char c, Image<Rgba32> image, FontGlyph info)>();

        int padding = 1;
        int maxHeight = 0;

        foreach (char c in charset)
        {
            var size = TextMeasurer.MeasureBounds(c.ToString(), options);
            int width = (int)Math.Ceiling(size.Width + padding);
            int height = (int)Math.Ceiling(size.Height + padding);
            maxHeight = Math.Max(maxHeight, height);

            var img = new Image<Rgba32>(width, height);
            img.Mutate(ctx =>
            {
                ctx.DrawText(options, c.ToString(), Color.White);
            });

            var info = new FontGlyph
            {
                OffsetX = padding,
                OffsetY = padding,
                AdvanceX = (int)Math.Ceiling(size.Width)
            };

            glyphImages.Add((c, img, info));
        }

        // Pack into single row
        int atlasWidth = glyphImages.Sum(g => g.image.Width);
        var atlas = new Image<Rgba32>(atlasWidth, maxHeight);
        int currentX = 0;

        foreach (var (c, image, info) in glyphImages)
        {
            var x = currentX;
            atlas.Mutate(ctx => ctx.DrawImage(image, new Point(x, 0), 1f));
            info.AtlasBounds = new Rectangle(currentX, 0, image.Width, image.Height);
            glyphs[c] = info;
            currentX += image.Width;
            image.Dispose();
        }

        return new BitmapFont
        {
            Atlas = atlas,
            Glyphs = glyphs,
            Font = font,
            LineHeight = maxHeight,
        };
    }

    public static (int, int) MeasureText(string text, string fontFamily, int fontSize)
    {
        var bitmapFont = GetOrCreateBitmapFont(fontFamily, fontSize);

        int width = 0;
        int height = 0;

        foreach (char c in text)
        {
            if (bitmapFont.Glyphs.TryGetValue(c, out FontGlyph? info))
            {
                width += info.AtlasBounds.Width;
                if (height < info.AtlasBounds.Height)
                {
                    height = info.AtlasBounds.Height;
                }
            }
        }

        return (width, height);
    }
}