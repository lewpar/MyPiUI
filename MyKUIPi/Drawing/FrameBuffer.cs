using System.IO.MemoryMappedFiles;

using MyKUIPi.Primitives;

namespace MyKUIPi.Drawing;

public class FrameBuffer : IDisposable
{
    private Rectangle? _clipRect;
    
    private readonly FrameBufferInfo _frameInfo;
    private MyEngineOptions _myOptions;

    private readonly FileStream _frameBufferStream;
    private readonly MemoryMappedFile _frameBufferMemoryMap;
    private readonly MemoryMappedViewAccessor _frameBufferAccessor;

    public List<Rectangle> DirtyRegions { get; }
    private readonly byte[] _softwareBackBuffer;
    private readonly int _bytesPerPixel;
    private readonly bool _is16Bit;

    public FrameBuffer(FrameBufferInfo frameInfo, MyEngineOptions myOptions)
    {
        _frameInfo = frameInfo;
        _myOptions = myOptions;

        _bytesPerPixel = frameInfo.Depth / 8;

        _is16Bit = frameInfo.Depth == 16;

        var frameBufferSize = frameInfo.Width * frameInfo.VirtualHeight * _bytesPerPixel;

        if (myOptions.RenderMode == RenderMode.FrameBuffer)
        {
            _frameBufferStream = new FileStream(myOptions.FrameBufferDevice, FileMode.Open, FileAccess.ReadWrite,
                FileShare.ReadWrite);
            _frameBufferMemoryMap = MemoryMappedFile.CreateFromFile(_frameBufferStream, null, frameBufferSize,
                MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false);
            _frameBufferAccessor =
                _frameBufferMemoryMap.CreateViewAccessor(0, frameBufferSize, MemoryMappedFileAccess.Write);
        }

        DirtyRegions = new List<Rectangle>();
        _softwareBackBuffer = new byte[_frameInfo.Width * _frameInfo.Height * _bytesPerPixel];
    }

    public byte[] GetBuffer()
    {
        return _softwareBackBuffer;
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
        int x0 = Math.Max(0, rect.X);
        int y0 = Math.Max(0, rect.Y);
        int x1 = Math.Min(_frameInfo.Width, rect.X + rect.Width);
        int y1 = Math.Min(_frameInfo.Height, rect.Y + rect.Height);

        byte[] rawColor = GetRawColor(color);

        int pitch = _frameInfo.Width * _bytesPerPixel;

        unsafe
        {
            fixed (byte* bufferPtr = _softwareBackBuffer)
            fixed (byte* colorPtr = rawColor)
            {
                for (int y = y0; y < y1; y++)
                {
                    byte* row = bufferPtr + (y * pitch) + (x0 * _bytesPerPixel);

                    for (int x = x0; x < x1; x++)
                    {
                        Buffer.MemoryCopy(colorPtr, row, _bytesPerPixel, _bytesPerPixel);
                        row += _bytesPerPixel;
                    }
                }
            }
        }
    }

    private byte[] GetRawColor(Color color)
    {
        return GetRawColor(color.R, color.G, color.B);
    }
    
    private byte[] GetRawColor(byte r, byte g, byte b)
    {
        return _is16Bit ?
            Color.To16Bit(r, g, b) :
            (_myOptions.RenderMode == RenderMode.Raylib ? 
                Color.ToRGBA(r, g, b) : 
                Color.ToBGRA(r, g, b));
    }

    public void Clear(Color color)
    {
        var rawColor = GetRawColor(color);

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
        if (x < 0 || x >= _frameInfo.Width || y < 0 || y >= _frameInfo.Height)
            return;

        if (_clipRect is not null && !_clipRect.Value.ContainsPoint(x, y))
            return;

        var rawColor = GetRawColor(r, g, b);
        int pixelOffset = (y * _frameInfo.Width + x) * _bytesPerPixel;

        unsafe
        {
            fixed (byte* bufferPtr = _softwareBackBuffer)
            fixed (byte* colorPtr = rawColor)
            {
                byte* dst = bufferPtr + pixelOffset;
                for (int i = 0; i < _bytesPerPixel; i++)
                {
                    dst[i] = colorPtr[i];
                }
            }
        }
    }

    private void DrawPixel(int x, int y, Color color)
    {
        DrawPixel(x, y, color.R, color.G, color.B);
    }
    
    public void DrawImage(int x, int y, BitmapImage image)
    {
        Color mask = Color.Fuchsia;
        int bytesPerPixel = _bytesPerPixel;
        int frameWidth = _frameInfo.Width;
        int frameHeight = _frameInfo.Height;
        int imgWidth = image.Width;
        int imgHeight = image.Height;
        byte[] pixelData = image.PixelData;
        int depth = _frameInfo.Depth;

        unsafe
        {
            fixed (byte* bufferPtr = _softwareBackBuffer)
            {
                for (int py = 0; py < imgHeight; py++)
                {
                    int destY = y + py;
                    if (destY < 0 || destY >= frameHeight)
                        continue;

                    for (int px = 0; px < imgWidth; px++)
                    {
                        int destX = x + px;
                        if (destX < 0 || destX >= frameWidth)
                            continue;

                        int srcIndex = (py * imgWidth + px) * (depth / 8);

                        byte r, g, b;

                        if (depth == 16)
                        {
                            ushort pixel = BitConverter.ToUInt16(pixelData, srcIndex);
                            r = (byte)(((pixel >> 11) & 0x1F) * 255 / 31);
                            g = (byte)(((pixel >> 5) & 0x3F) * 255 / 63);
                            b = (byte)((pixel & 0x1F) * 255 / 31);
                        }
                        else if (depth == 32)
                        {
                            if (_myOptions.RenderMode == RenderMode.Raylib)
                            {
                                r = pixelData[srcIndex + 0];
                                g = pixelData[srcIndex + 1];
                                b = pixelData[srcIndex + 2];
                            }
                            else
                            {
                                b = pixelData[srcIndex + 0];
                                g = pixelData[srcIndex + 1];
                                r = pixelData[srcIndex + 2];   
                            }
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported bpp: {depth}");
                        }

                        if (r == mask.R && g == mask.G && b == mask.B)
                            continue;

                        int destOffset = (destY * frameWidth + destX) * bytesPerPixel;
                        byte* dst = bufferPtr + destOffset;

                        if (_is16Bit)
                        {
                            ushort raw = Color.To16BitValue(r, g, b);
                            dst[0] = (byte)(raw & 0xFF);
                            dst[1] = (byte)(raw >> 8);
                        }
                        else
                        {
                            dst[0] = b;
                            dst[1] = g;
                            dst[2] = r;
                            dst[3] = 0xFF;
                        }
                    }
                }
            }
        }

        DirtyRegions.Add(new Rectangle(x, y, imgWidth, imgHeight));
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
        int x0 = Math.Max(0, x);
        int y0 = Math.Max(0, y);
        int x1 = Math.Min(_frameInfo.Width, x + width);
        int y1 = Math.Min(_frameInfo.Height, y + height);

        byte[] rawColor = GetRawColor(color);

        int pitch = _frameInfo.Width * _bytesPerPixel;

        unsafe
        {
            fixed (byte* bufferPtr = _softwareBackBuffer)
            fixed (byte* colorPtr = rawColor)
            {
                for (int py = y0; py < y1; py++)
                {
                    byte* row = bufferPtr + (py * pitch) + (x0 * _bytesPerPixel);

                    for (int px = x0; px < x1; px++)
                    {
                        for (int b = 0; b < _bytesPerPixel; b++)
                        {
                            row[b] = colorPtr[b];
                        }
                        row += _bytesPerPixel;
                    }
                }
            }
        }
    }
    
    public void DrawLine(int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            DrawPixel(x0, y0, color);

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
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
            font = FrameBufferFont.Basic8x8[' '];

        float scale = fontSize / 8f;
        int intScale = (int)scale;
        int width = _frameInfo.Width;
        int height = _frameInfo.Height;
        int pitch = width * _bytesPerPixel;
        byte[] rawColor = GetRawColor(color);

        unsafe
        {
            fixed (byte* bufferPtr = _softwareBackBuffer)
            fixed (byte* colorPtr = rawColor)
            {
                for (int row = 0; row < 8; row++)
                {
                    byte bits = font[row];

                    for (int col = 0; col < 8; col++)
                    {
                        if ((bits & (1 << (7 - col))) == 0)
                            continue;

                        int px = x + (int)(col * scale);
                        int py = y + (int)(row * scale);

                        for (int dy = 0; dy < intScale; dy++)
                        {
                            int dstY = py + dy;
                            if (dstY < 0 || dstY >= height)
                                continue;

                            byte* dstRow = bufferPtr + dstY * pitch;

                            for (int dx = 0; dx < intScale; dx++)
                            {
                                int dstX = px + dx;
                                if (dstX < 0 || dstX >= width)
                                    continue;

                                byte* pixelPtr = dstRow + dstX * _bytesPerPixel;

                                for (int b = 0; b < _bytesPerPixel; b++)
                                    pixelPtr[b] = colorPtr[b];
                            }
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
        unsafe
        {
            fixed (byte* src = _softwareBackBuffer)
            {
                byte* dst = null;
                _frameBufferAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref dst);
                try
                {
                    Buffer.MemoryCopy(src, dst, _softwareBackBuffer.Length, _softwareBackBuffer.Length);
                }
                finally
                {
                    _frameBufferAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                }
            }
        }
    }
    
    public void Dispose()
    {
        _frameBufferAccessor.Dispose();
        _frameBufferMemoryMap.Dispose();
        _frameBufferStream.Dispose();
    }
}