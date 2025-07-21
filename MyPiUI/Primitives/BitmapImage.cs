using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MyPiUI.Primitives;

public class BitmapImage
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public byte[] PixelData { get; private set; }
    public byte[]? AlphaData { get; private set; }

    private BitmapImage(int width, int height, byte[] pixelData,  byte[]? alphaData)
    {
        Width = width;
        Height = height;
        PixelData = pixelData;
        AlphaData = alphaData;
    }

    public static BitmapImage Load(string path, int bpp, int width, int height)
    {
        if (bpp != 16 && bpp != 32)
            throw new ArgumentException("Only 16 or 32 bits per pixel supported.");

        using var image = Image.Load<Argb32>(path);

        image.Mutate(ctx => ctx.Resize(width, height));

        if (bpp == 16)
        {
            var result = ConvertToRgb565WithAlpha(image, width, height);
            return new BitmapImage(width, height, result.rgb565, result.alpha);
        }

        byte[] bgra = ConvertToBgra32(image, width, height);
        return new BitmapImage(width, height, bgra, null);
    }
    
    private static (byte[] rgb565, byte[] alpha) ConvertToRgb565WithAlpha(Image<Argb32> image, int width, int height)
    {
        byte[] rgb565 = new byte[width * height * 2];
        byte[] alpha = new byte[width * height];

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    var pixel = row[x];
                    ushort r = (ushort)(pixel.R >> 3);
                    ushort g = (ushort)(pixel.G >> 2);
                    ushort b = (ushort)(pixel.B >> 3);
                    ushort packed = (ushort)((r << 11) | (g << 5) | b);

                    int index = y * width + x;
                    rgb565[index * 2 + 0] = (byte)(packed & 0xFF);
                    rgb565[index * 2 + 1] = (byte)((packed >> 8) & 0xFF);
                    alpha[index] = pixel.A;
                }
            }
        });

        return (rgb565, alpha);
    }
    
    private static byte[] ConvertToBgra32(Image<Argb32> image, int width, int height)
    {
        byte[] data = new byte[width * height * 4];

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    var pixel = row[x];
                    int index = (y * width + x) * 4;
                    data[index + 0] = pixel.B;
                    data[index + 1] = pixel.G;
                    data[index + 2] = pixel.R;
                    data[index + 3] = pixel.A;
                }
            }
        });

        return data;
    }
}