using System.IO.MemoryMappedFiles;
using MyKUIPi.Primitives;

namespace MyKUIPi.Drawing;

public class FrameBuffer : IDisposable
{
    private Rectangle? _clipRect;
    
    private FrameBufferInfo _frameInfo;
    private MyEngineOptions _myOptions;

    private FileStream _frameBufferStream;
    private MemoryMappedFile _frameBufferMemoryMap;
    private MemoryMappedViewAccessor _frameBufferAccessor;

    public List<Rectangle> DirtyRegions { get; }
    private byte[] _softwareBackBuffer;
    private int _bytesPerPixel;
    private bool _is16Bit;

    public FrameBuffer(FrameBufferInfo frameInfo, MyEngineOptions myOptions)
    {
        _frameInfo = frameInfo;
        _myOptions = myOptions;

        _bytesPerPixel = frameInfo.Depth / 8;

        _is16Bit = frameInfo.Depth == 16;

        var frameBufferSize = frameInfo.Width * frameInfo.VirtualHeight * _bytesPerPixel;

        _frameBufferStream = new FileStream(myOptions.FrameBufferDevice, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        _frameBufferMemoryMap = MemoryMappedFile.CreateFromFile(_frameBufferStream, null, frameBufferSize, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
        _frameBufferAccessor = _frameBufferMemoryMap.CreateViewAccessor(0, frameBufferSize, MemoryMappedFileAccess.Write);

        DirtyRegions = new List<Rectangle>();
        _softwareBackBuffer = new byte[_frameInfo.Width * _frameInfo.Height * _bytesPerPixel];
    }

    public void SetClip(Rectangle clipRect)
    {
        _clipRect = clipRect;
    }

    public void ClearClip()
    {
        _clipRect = null;
    }

    public void Clear(Color color, Rectangle rect)
    {
        FillRectNoDirty(rect.X, rect.Y, rect.Width, rect.Height, color);
    }

    public void Clear(Color color)
    {
        var rawColor = _is16Bit ?
            Color.To16Bit(color) :
            Color.ToLittleEndian(color);

        for (int i = 0; i < _softwareBackBuffer.Length; i += _bytesPerPixel)
        {
            for (int j = 0; j < _bytesPerPixel; j++)
            {
                _softwareBackBuffer[i + j] = rawColor[j];
            }
        }
    }

    private void DrawPixel(int x, int y, byte r, byte g, byte b)
    {
        if (x < 0 || x >= _frameInfo.Width ||
            y < 0 || y >= _frameInfo.Height)
        {
            return;
        }

        if (_clipRect is not null && !_clipRect.Value.ContainsPoint(x, y))
        {
            return;
        }

        var rawColor = _is16Bit ?
            Color.To16Bit(r, g, b) :
            Color.ToLittleEndian(r, g, b);

        var pixelOffset = (y * _frameInfo.Width + x) * _bytesPerPixel;

        Buffer.BlockCopy(rawColor, 0, _softwareBackBuffer, pixelOffset, _bytesPerPixel);
    }

    private void DrawPixel(int x, int y, Color color)
    {
        if (x < 0 || x >= _frameInfo.Width ||
            y < 0 || y >= _frameInfo.Height)
        {
            return;
        }

        if (_clipRect is not null && !_clipRect.Value.ContainsPoint(x, y))
        {
            return;
        }

        var rawColor = _is16Bit ?
            Color.To16Bit(color) :
            Color.ToLittleEndian(color);

        var pixelOffset = (y * _frameInfo.Width + x) * _bytesPerPixel;

        Buffer.BlockCopy(rawColor, 0, _softwareBackBuffer, pixelOffset, _bytesPerPixel);
    }
    
    public void DrawImage(int x, int y, BitmapImage image)
    {
        Color mask = Color.Fuchsia;

        for (int py = 0; py < image.Height; py++)
        {
            int destY = y + py;
            if (destY < 0 || destY >= _frameInfo.Height)
                continue;

            for (int px = 0; px < image.Width; px++)
            {
                int destX = x + px;
                if (destX < 0 || destX >= _frameInfo.Width)
                    continue;

                byte r = 0;
                byte g = 0;
                byte b = 0;

                if (_frameInfo.Depth == 16)
                {
                    int srcIndex = (py * image.Width + px) * 2;
                    ushort pixel = BitConverter.ToUInt16(image.PixelData, srcIndex);

                    r = (byte)(((pixel >> 11) & 0x1F) * 255 / 31);
                    g = (byte)(((pixel >> 5) & 0x3F) * 255 / 63);
                    b = (byte)((pixel & 0x1F) * 255 / 31);
                }
                else if (_frameInfo.Depth == 32)
                {
                    int srcIndex = (py * image.Width + px) * 4;
                    b = image.PixelData[srcIndex + 0];
                    g = image.PixelData[srcIndex + 1];
                    r = image.PixelData[srcIndex + 2];
                }
                else
                {
                    throw new NotSupportedException($"Unsupported bpp: {_frameInfo.Depth}");
                }

                if (mask.R == r && mask.G == g && mask.B == b)
                {
                    continue;
                }

                DrawPixel(destX, destY, r, g, b);
            }
        }

        DirtyRegions.Add(new Rectangle(x, y, image.Width, image.Height));
    }

    public void FillTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Color color)
    {
        // Sort points by Y ascending (p1.Y <= p2.Y <= p3.Y)
        if (p2.Y < p1.Y) (p1, p2) = (p2, p1);
        if (p3.Y < p1.Y) (p1, p3) = (p3, p1);
        if (p3.Y < p2.Y) (p2, p3) = (p3, p2);

        // Compute inverse slopes
        float dx1 = 0, dx2 = 0, dx3 = 0;

        if (p2.Y - p1.Y > 0)
            dx1 = (p2.X - p1.X) / (p2.Y - p1.Y);
        if (p3.Y - p1.Y > 0)
            dx2 = (p3.X - p1.X) / (p3.Y - p1.Y);
        if (p3.Y - p2.Y > 0)
            dx3 = (p3.X - p2.X) / (p3.Y - p2.Y);

        var sx = p1.X;
        var ex = p1.X;

        // Draw upper part of triangle (flat bottom)
        for (int y = (int)p1.Y; y <= p2.Y; y++)
        {
            if (y < 0 || y >= _frameInfo.Height)
            {
                sx += dx1;
                ex += dx2;
                continue;
            }

            int startX = (int)Math.Round(sx);
            int endX = (int)Math.Round(ex);

            if (startX > endX)
                (startX, endX) = (endX, startX);

            for (int x = startX; x <= endX; x++)
            {
                if (x >= 0 && x < _frameInfo.Width)
                    DrawPixel(x, y, color);
            }

            sx += dx1;
            ex += dx2;
        }

        sx = p2.X;
        // ex continues from previous loop (p1 to p3)
        // Reset ex to p1.X + dx2 * (p2.Y - p1.Y)
        ex = p1.X + dx2 * (p2.Y - p1.Y);

        // Draw lower part of triangle (flat top)
        for (int y = (int)p2.Y; y <= p3.Y; y++)
        {
            if (y < 0 || y >= _frameInfo.Height)
            {
                sx += dx3;
                ex += dx2;
                continue;
            }

            int startX = (int)Math.Round(sx);
            int endX = (int)Math.Round(ex);

            if (startX > endX)
                (startX, endX) = (endX, startX);

            for (int x = startX; x <= endX; x++)
            {
                if (x >= 0 && x < _frameInfo.Width)
                    DrawPixel(x, y, color);
            }

            sx += dx3;
            ex += dx2;
        }
    }

    public void DrawRect(int x, int y, int width, int height, int borderWidth, Color color)
    {
        if (borderWidth < 1)
        {
            return;
        }

        if (borderWidth > 10)
        {
            borderWidth = 10;
        }

        for (int i = 0; i < borderWidth; i++)
        {
            int topY = y + i;
            int bottomY = y + height - 1 - i;
            int leftX = x + i;
            int rightX = x + width - 1 - i;

            // Draw top horizontal line
            if (topY >= 0 && topY < _frameInfo.Height)
            {
                for (int px = leftX; px <= rightX; px++)
                {
                    if (px < 0 || px >= _frameInfo.Width) continue;
                    DrawPixel(px, topY, color);
                }
            }

            // Draw bottom horizontal line (if different from top)
            if (bottomY != topY && bottomY >= 0 && bottomY < _frameInfo.Height)
            {
                for (int px = leftX; px <= rightX; px++)
                {
                    if (px < 0 || px >= _frameInfo.Width) continue;
                    DrawPixel(px, bottomY, color);
                }
            }

            // Draw left vertical line
            if (leftX >= 0 && leftX < _frameInfo.Width)
            {
                for (int py = topY; py <= bottomY; py++)
                {
                    if (py < 0 || py >= _frameInfo.Height) continue;
                    DrawPixel(leftX, py, color);
                }
            }

            // Draw right vertical line (if different from left)
            if (rightX != leftX && rightX >= 0 && rightX < _frameInfo.Width)
            {
                for (int py = topY; py <= bottomY; py++)
                {
                    if (py < 0 || py >= _frameInfo.Height) continue;
                    DrawPixel(rightX, py, color);
                }
            }
        }
        
        DirtyRegions.Add(new Rectangle(x, y, width, borderWidth));                         // Top border
        DirtyRegions.Add(new Rectangle(x, y + height - borderWidth, width, borderWidth));  // Bottom border
        DirtyRegions.Add(new Rectangle(x, y + borderWidth, borderWidth, height - 2 * borderWidth));  // Left border
        DirtyRegions.Add(new Rectangle(x + width - borderWidth, y + borderWidth, borderWidth, height - 2 * borderWidth)); // Right border
    }

    private void FillRectNoDirty(int x, int y, int width, int height, Color color)
    {
        for (int py = y; py < y + height; py++)
        {
            if (py < 0 || py >= _frameInfo.Height)
            {
                continue;
            }

            for (int px = x; px < x + width; px++)
            {
                if (px < 0 || px >= _frameInfo.Width)
                {
                    continue;
                }

                DrawPixel(px, py, color);
            }
        }
    }

    public void FillRect(int x, int y, int width, int height, Color color)
    {
        FillRectNoDirty(x, y, width, height, color);
        DirtyRegions.Add(new Rectangle(x, y, width, height));
    }
    public void FillRect(Rectangle rect, Color color)
    {
        FillRect(rect.X, rect.Y, rect.Width, rect.Height, color);
    }

    private void DrawChar(int x, int y, char character, Color color, int fontSize = 8)
    {
        if (fontSize < 8)
            fontSize = 8;

        if (!FrameBufferFont.Basic8x8.TryGetValue(character, out var font))
        {
            font = FrameBufferFont.Basic8x8[' '];
        }

        // Calculate the pixel scaling factor based on font size
        float scale = fontSize / 8f;

        for (int row = 0; row < 8; row++)
        {
            byte bits = font[row];
            for (int col = 0; col < 8; col++)
            {
                if ((bits & (1 << (7 - col))) != 0)
                {
                    // Draw a block scaled to the requested font size
                    int px = x + (int)(col * scale);
                    int py = y + (int)(row * scale);

                    // Draw the scaled pixel (as a filled rectangle)
                    for (int dy = 0; dy < scale; dy++)
                    {
                        for (int dx = 0; dx < scale; dx++)
                        {
                            DrawPixel(px + dx, py + dy, color);
                        }
                    }
                }
            }
        }
    }

    public void DrawText(int x, int y, string text, Color color, int fontSize = 8)
    {
        if (fontSize < 8)
            fontSize = 8;

        int cellWidth = fontSize;

        for (int i = 0; i < text.Length; i++)
        {
            int charX = x + i * cellWidth;
            DrawChar(charX, y, text[i], color, fontSize);
        }

        DirtyRegions.Add(new Rectangle(x, y, text.Length * fontSize, fontSize));
    }
    
    public void SwapBuffers()
    {
        _frameBufferAccessor.WriteArray(0, _softwareBackBuffer, 0, _softwareBackBuffer.Length);
    }

    public void Dispose()
    {
        _frameBufferAccessor.Dispose();
        _frameBufferMemoryMap.Dispose();
        _frameBufferStream.Dispose();
    }
}