using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MyKUIPi.Primitives;

public class BitmapImage
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public byte[] PixelData { get; private set; }

    private BitmapImage(int width, int height, byte[] pixelData)
    {
        Width = width;
        Height = height;
        PixelData = pixelData;
    }

    public static BitmapImage Load(string path, int bpp)
    {
        if (bpp != 16 && bpp != 32)
            throw new ArgumentException("Only 16 or 32 bits per pixel supported.");

        using var image = Image.Load<Rgb24>(path);
        int width = image.Width;
        int height = image.Height;

        byte[] pixelData = bpp switch
        {
            
            16 => ConvertToRGB565(image, width, height),
            32 => ConvertToBGRA32(image, width, height),
            _ => throw new NotSupportedException("Unsupported bpp.")
        };

        return new BitmapImage(width, height, pixelData);
    }
    
    private static byte[] ConvertToRGB565(Image<Rgb24> image, int width, int height)
    {
        byte[] data = new byte[width * height * 2];

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
                    ushort rgb565 = (ushort)((r << 11) | (g << 5) | b);

                    int index = (y * width + x) * 2;
                    data[index + 0] = (byte)(rgb565 & 0xFF);
                    data[index + 1] = (byte)((rgb565 >> 8) & 0xFF);
                }
            }
        });

        return data;
    }
    
    private static byte[] ConvertToBGRA32(Image<Rgb24> image, int width, int height)
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
                    data[index + 3] = 0xFF; // Full alpha
                }
            }
        });

        return data;
    }
}