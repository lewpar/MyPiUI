namespace MyKUIPi.Primitives;

public struct Color
{
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }

    public Color(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }

    public override string ToString()
    {
        return $"rgb({R}, {G}, {B})";
    }

    public static Color FromHex(string hex)
    {
        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (hex.Length != 6)
            throw new FormatException("Hex color must be 6 characters.");

        return new Color(
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16)
        );
    }
    
    public static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
    
    public static byte[] ToLittleEndian(Color color)
    {
        return new byte[] { color.B, color.G, color.R, 255 /* UNUSED ALPHA CHANNEL */ };
    }

    public static byte[] To16Bit(Color color)
    {
        // Convert 8-bit RGB to 16-bit RGB565 format
        // R: 5 bits (0-31), G: 6 bits (0-63), B: 5 bits (0-31)
        ushort red = (ushort)((color.R >> 3) & 0x1F);   // 5 bits
        ushort green = (ushort)((color.G >> 2) & 0x3F); // 6 bits  
        ushort blue = (ushort)((color.B >> 3) & 0x1F);  // 5 bits

        // Pack into 16-bit value: RRRRRGGGGGGBBBBB
        ushort color16 = (ushort)((red << 11) | (green << 5) | blue);

        // Convert to little-endian bytes
        return new byte[] { (byte)(color16 & 0xFF), (byte)((color16 >> 8) & 0xFF) };
    }

    public static Color Black => new Color(0, 0, 0);
    public static Color White => new Color(255, 255, 255);
    public static Color Gray => new Color(50, 50, 50);
    public static Color Orange => new Color(255, 165, 0);
    public static Color Red => new Color(255, 0, 0);
    public static Color Green => new Color(0, 255, 0);
    public static Color Blue => new Color(0, 0, 255);
    public static Color DodgerBlue => new Color(30, 144, 255);
    public static Color SkyBlue => new Color(25, 62, 100);
}