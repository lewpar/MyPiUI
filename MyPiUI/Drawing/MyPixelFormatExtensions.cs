namespace MyPiUI.Drawing;

public static class MyPixelFormatExtensions
{
    public static int GetBitsPerPixel(this MyPixelFormat format)
    {
        switch (format)
        {
            case MyPixelFormat.B8G8R8A8:
            case MyPixelFormat.R8G8B8A8:
                return 32;
            
            case MyPixelFormat.R5G6B5:
                return 16;
        }
        
        throw new Exception("Unsupported PixelFormat.");
    }
}