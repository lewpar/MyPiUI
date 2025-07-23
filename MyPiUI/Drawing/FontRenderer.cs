using System.Runtime.InteropServices;
using static StbTrueTypeSharp.StbTrueType;

namespace MyPiUI.Drawing;

public static unsafe class FontRenderer
{
    private static readonly Dictionary<int, stbtt_fontinfo> Fonts = new();
    private static readonly Dictionary<int, byte[]> FontData = new();
    private static readonly Dictionary<int, GCHandle> FontDataHandles = new();
    private static readonly Dictionary<int, float> FontScales = new();
    private static readonly Dictionary<int, int> FontAscents = new();

    private const string FontPath = "Fonts/RobotoMono-Regular.ttf";

    public static void InitializeFont(int fontSize)
    {
        if (Fonts.ContainsKey(fontSize)) return; // Already loaded

        var data = File.ReadAllBytes(FontPath);
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        FontDataHandles[fontSize] = handle;

        byte* ptr = (byte*)handle.AddrOfPinnedObject();

        var font = new stbtt_fontinfo();
        if (stbtt_InitFont(font, ptr, 0) == 0)
            throw new Exception($"Failed to load font: {FontPath}");

        Fonts[fontSize] = font;
        FontData[fontSize] = data;

        var scale = stbtt_ScaleForPixelHeight(font, fontSize);
        FontScales[fontSize] = scale;

        int ascent, descent, lineGap;
        stbtt_GetFontVMetrics(font, &ascent, &descent, &lineGap);
        FontAscents[fontSize] = (int)(ascent * scale);
    }

    public static void DrawText(DrawBuffer buffer, int x, int y, string text, int fontSize)
    {
        if (fontSize <= 0)
        {
            return;
        }
        
        if (!Fonts.ContainsKey(fontSize)) InitializeFont(fontSize);

        var font = Fonts[fontSize];
        var scale = FontScales[fontSize];
        var ascent = FontAscents[fontSize];
        var data = FontData[fontSize];

        int posX = x;
        int posY = y;

        fixed (byte* dataPtr = data)
        {
            foreach (char c in text)
            {
                int glyphIndex = stbtt_FindGlyphIndex(font, c);
                if (glyphIndex == 0) continue;

                int advance, lsb;
                stbtt_GetGlyphHMetrics(font, glyphIndex, &advance, &lsb);

                int x0, y0, x1, y1;
                stbtt_GetGlyphBitmapBox(font, glyphIndex, scale, scale, &x0, &y0, &x1, &y1);

                int width = x1 - x0;
                int height = y1 - y0;
                
                if (width <= 0 || height <= 0) continue;
                byte[] pixels = new byte[width * height];

                fixed (byte* pixelsPtr = pixels)
                {
                    stbtt_MakeGlyphBitmapSubpixel(font, pixelsPtr, width, height, width, scale, scale, 0, 0, glyphIndex);
                }

                for (int row = 0; row < height; row++)
                {
                    for (int col = 0; col < width; col++)
                    {
                        byte alpha = pixels[row * width + col];
                        if (alpha == 0) continue;

                        int drawX = posX + x0 + col;
                        int drawY = posY + ascent + y0 + row;

                        buffer.DrawPixel(drawX, drawY, 255, 255, 255, alpha);
                    }
                }

                posX += (int)(advance * scale + 0.5f);
            }
        }
    }

    public static (int width, int height) MeasureText(string text, int fontSize)
    {
        if (fontSize <= 0)
        {
            return (0, 0);
        }
        
        if (!Fonts.ContainsKey(fontSize)) InitializeFont(fontSize);

        var font = Fonts[fontSize];
        var scale = FontScales[fontSize];

        int ascent, descent, lineGap;
        stbtt_GetFontVMetrics(font, &ascent, &descent, &lineGap);

        int width = 0;
        foreach (char c in text)
        {
            int glyphIndex = stbtt_FindGlyphIndex(font, c);
            if (glyphIndex == 0) continue;

            int advance, lsb;
            stbtt_GetGlyphHMetrics(font, glyphIndex, &advance, &lsb);

            width += (int)(advance * scale + 0.5f);
        }

        int height = (int)((ascent - descent + lineGap) * scale);

        return (width, height);
    }

    public static void Cleanup()
    {
        foreach (var handle in FontDataHandles.Values)
        {
            if (handle.IsAllocated) handle.Free();
        }
        FontDataHandles.Clear();
        Fonts.Clear();
        FontData.Clear();
        FontScales.Clear();
        FontAscents.Clear();
    }
}
