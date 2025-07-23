using MyPiUI.Primitives;

using Color = MyPiUI.Primitives.Color;
using Rectangle = MyPiUI.Primitives.Rectangle;

namespace MyPiUI.Drawing;

public class DrawBuffer
{
    private readonly int _width;
    private readonly int _height;
    
    private readonly MyPixelFormat _myPixelFormat;
    
    private readonly int _bitsPerPixel;
    private readonly int _bytesPerPixel;
    
    private byte[] _backBuffer;

    private List<Rectangle> _dirtyRegions;
    
    private Color _clearColor;
    
    private Rectangle? _clipRect;

    public DrawBuffer(MyGraphicsContext graphicsContext)
    {
        _width = graphicsContext.Width;
        _height = graphicsContext.Height;
        
        _myPixelFormat = graphicsContext.PixelFormat;
        
        _bitsPerPixel = graphicsContext.BitsPerPixel;
        _bytesPerPixel =  _bitsPerPixel / 8;
        
        _backBuffer = new byte[_width * _height * _bytesPerPixel];
        
        _dirtyRegions = new List<Rectangle>();
        
        _clearColor = Color.Black;
    }

    public byte[] GetBuffer()
    {
        return _backBuffer;
    }

    private byte[] GetRawColor(Color color)
    {
        return GetRawColor(color.R, color.G, color.B);
    }

    private byte[] GetRawColor(byte r, byte g, byte b)
    {
        switch (_myPixelFormat)
        {
            case MyPixelFormat.R5G6B5:
                return Color.To16Bit(r, g, b);
            
            case MyPixelFormat.R8G8B8A8:
                return Color.ToRgba(r, g, b);
            
            case MyPixelFormat.B8G8R8A8:
                return Color.ToBgra(r, g, b);
            
            default:
                throw new Exception("Invalid pixel format.");
        }
    }

    /// <summary>
    /// Sets the back buffer clear color.
    /// </summary>
    /// <param name="color">The clear color.</param>
    public void SetClearColor(Color color)
    {
        _clearColor = color;
    }

    /// <summary>
    /// Sets the clipping rectangle for the buffer. Areas outside this area are not drawn until <see cref="ClearClipRect"/> is called.
    /// </summary>
    /// <param name="rect">The region to clip.</param>
    public void SetClipRect(Rectangle rect)
    {
        _clipRect = rect;
    }
    
    /// <summary>
    /// Clearss the clipping rectangle for the buffer. Used in conjunction with <see cref="SetClipRect"/>.
    /// </summary>
    public void ClearClipRect()
    {
        _clipRect = null;
    }
    
    /// <summary>
    /// Clears the entire screen.
    /// </summary>
    public void Clear()
    {
        var rawColor = GetRawColor(_clearColor);

        for (int i = 0; i < _backBuffer.Length; i += _bitsPerPixel)
        {
            for (int j = 0; j < _bytesPerPixel; j++)
            {
                _backBuffer[i + j] = rawColor[j];
            }
        }
    }
    
    /// <summary>
    /// Clear a region on the screen.
    /// </summary>
    /// <param name="rect">The region to clear.</param>
    public void Clear(Rectangle rect)
    {
        int x0 = Math.Max(0, rect.X);
        int y0 = Math.Max(0, rect.Y);
        int x1 = Math.Min(_width, rect.X + rect.Width);
        int y1 = Math.Min(_height, rect.Y + rect.Height);

        byte[] rawColor = GetRawColor(_clearColor);

        int pitch = _width * _bytesPerPixel;

        unsafe
        {
            fixed (byte* bufferPtr = _backBuffer)
            fixed (byte* colorPtr = rawColor)
            {
                for (int y = y0; y < y1; y++)
                {
                    byte* row = bufferPtr + (y * pitch) + (x0 * _bytesPerPixel);

                    for (int x = x0; x < x1; x++)
                    {
                        Buffer.MemoryCopy(colorPtr, row, _bitsPerPixel, _bitsPerPixel);
                        row += _bytesPerPixel;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Clear the dirty regions on the screen using the draw buffer clear color.
    /// </summary>
    public void ClearDirtyRegions()
    {
        foreach (var dirtyRegion in _dirtyRegions)
        {
            Clear(dirtyRegion);
        }
        
        _dirtyRegions.Clear();
    }
    
    /// <summary>
    /// Draws a pixel on the screen.
    /// </summary>
    /// <param name="x">The pixels x coordinate on the screen.</param>
    /// <param name="y">The pixels y coordinate on the screen.</param>
    /// <param name="r">The red color channel for the pixel.</param>
    /// <param name="g">The green color channel for the pixel.</param>
    /// <param name="b">The blue color channel for the pixel.</param>
    /// <param name="a">The alpha channel for the pixel.</param>
    public void DrawPixel(int x, int y, byte r, byte g, byte b, byte a = 255)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            return;

        if (_clipRect is not null && !_clipRect.Value.ContainsPoint(x, y))
            return;

        int pixelOffset = (y * _width + x) * _bytesPerPixel;

        unsafe
        {
            fixed (byte* bufferPtr = _backBuffer)
            {
                byte* dst = bufferPtr + pixelOffset;

                // Alpha blend with background
                if (a < 255)
                {
                    byte dstR = dst[0];
                    byte dstG = dst[1];
                    byte dstB = dst[2];

                    float alpha = a / 255f;
                    dst[0] = (byte)(r * alpha + dstR * (1 - alpha));
                    dst[1] = (byte)(g * alpha + dstG * (1 - alpha));
                    dst[2] = (byte)(b * alpha + dstB * (1 - alpha));
                }
                else
                {
                    dst[0] = r;
                    dst[1] = g;
                    dst[2] = b;
                }
            }
        }
    }
    
    /// <summary>
    /// Draws a pixel on the screen.
    /// </summary>
    /// <param name="x">The pixels x coordinate on the screen.</param>
    /// <param name="y">The pixels y coordinate on the screen.</param>
    /// <param name="color">The color for the pixel.</param>
    public void DrawPixel(int x, int y, Color color)
    {
        DrawPixel(x, y, color.R, color.G, color.B);
    }
    
    /// <summary>
    /// Draws a line between two points on the screen.
    /// </summary>
    /// <param name="x0">The first points x coordinate on the screen.</param>
    /// <param name="y0">The first points y coordinate on the screen.</param>
    /// <param name="x1">The second points x coordinate on the screen.</param>
    /// <param name="y1">The second points y coordinate on the screen.</param>
    /// <param name="color">The color of the line.</param>
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
    
    /// <summary>
    /// Fills a triangle on the screen.
    /// </summary>
    /// <param name="p1">The first triangle point.</param>
    /// <param name="p2">The second triangle point.</param>
    /// <param name="p3">The third triangle point.</param>
    /// <param name="color">The color of the triangle.</param>
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
            if (y < 0 || y >= _height)
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
                if (x >= 0 && x < _width)
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
            if (y < 0 || y >= _height)
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
                if (x >= 0 && x < _width)
                    DrawPixel(x, y, color);
            }

            sx += dx3;
            ex += dx2;
        }
    }
    
    /// <summary>
    /// Draws a rectangle outline on the screen.
    /// </summary>
    /// <param name="x">The rectangles x coordinate on the screen.</param>
    /// <param name="y">The rectangles y coordinate on the screen.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <param name="borderWidth">The width of the border line.</param>
    /// <param name="borderColor">The color of the outline</param>
    public void DrawRect(int x, int y, int width, int height, int borderWidth, Color borderColor)
    {
        if (borderWidth < 1) return;
        if (borderWidth > 10) borderWidth = 10;

        byte[] rawColor = GetRawColor(borderColor);
        int pitch = _width * _bytesPerPixel;

        unsafe
        {
            fixed (byte* bufferPtr = _backBuffer)
            fixed (byte* colorPtr = rawColor)
            {
                for (int i = 0; i < borderWidth; i++)
                {
                    int topY = y + i;
                    int bottomY = y + height - 1 - i;
                    int leftX = x + i;
                    int rightX = x + width - 1 - i;

                    // Top border
                    if (topY >= 0 && topY < _height)
                    {
                        byte* rowPtr = bufferPtr + topY * pitch;
                        for (int px = leftX; px <= rightX; px++)
                        {
                            if (px < 0 || px >= _width) continue;
                            byte* pixel = rowPtr + px * _bytesPerPixel;
                            for (int b = 0; b < _bytesPerPixel; b++)
                                pixel[b] = colorPtr[b];
                        }
                    }

                    // Bottom border
                    if (bottomY != topY && bottomY >= 0 && bottomY < _height)
                    {
                        byte* rowPtr = bufferPtr + bottomY * pitch;
                        for (int px = leftX; px <= rightX; px++)
                        {
                            if (px < 0 || px >= _width) continue;
                            byte* pixel = rowPtr + px * _bytesPerPixel;
                            for (int b = 0; b < _bytesPerPixel; b++)
                                pixel[b] = colorPtr[b];
                        }
                    }

                    // Left & right borders
                    for (int py = topY; py <= bottomY; py++)
                    {
                        if (py < 0 || py >= _height) continue;
                        byte* rowPtr = bufferPtr + py * pitch;

                        // Left border
                        if (leftX >= 0 && leftX < _width)
                        {
                            byte* pixel = rowPtr + leftX * _bytesPerPixel;
                            for (int b = 0; b < _bytesPerPixel; b++)
                                pixel[b] = colorPtr[b];
                        }

                        // Right border
                        if (rightX != leftX && rightX >= 0 && rightX < _width)
                        {
                            byte* pixel = rowPtr + rightX * _bytesPerPixel;
                            for (int b = 0; b < _bytesPerPixel; b++)
                                pixel[b] = colorPtr[b];
                        }
                    }
                }
            }
        }

        // Track dirty regions
        _dirtyRegions.Add(new Rectangle(x, y, width, borderWidth)); // Top
        _dirtyRegions.Add(new Rectangle(x, y + height - borderWidth, width, borderWidth)); // Bottom
        _dirtyRegions.Add(new Rectangle(x, y + borderWidth, borderWidth, height - 2 * borderWidth)); // Left
        _dirtyRegions.Add(new Rectangle(x + width - borderWidth, y + borderWidth, borderWidth, height - 2 * borderWidth)); // Right
    }

    
    /// <summary>
    /// Fills a rectangle on the screen, optionally with rounded corners.
    /// </summary>
    /// <param name="x">The rectangle's x coordinate on the screen.</param>
    /// <param name="y">The rectangle's y coordinate on the screen.</param>
    /// <param name="width">The width of the rectangle.</param>
    /// <param name="height">The height of the rectangle.</param>
    /// <param name="color">The color of the rectangle.</param>
    /// <param name="cornerRadius">The radius of the rounded corners (optional).</param>
    public void FillRect(int x, int y, int width, int height, Color color, int cornerRadius = 0)
    {
        int x0 = Math.Max(0, x);
        int y0 = Math.Max(0, y);
        int x1 = Math.Min(_width, x + width);
        int y1 = Math.Min(_height, y + height);

        byte[] rawColor = GetRawColor(color);
        int pitch = _width * _bytesPerPixel;

        int r = Math.Max(0, cornerRadius);
        int r2 = r * r; // squared radius for distance comparison

        unsafe
        {
            fixed (byte* bufferPtr = _backBuffer)
            fixed (byte* colorPtr = rawColor)
            {
                for (int py = y0; py < y1; py++)
                {
                    for (int px = x0; px < x1; px++)
                    {
                        bool draw = true;

                        // Check corners if cornerRadius > 0
                        if (r > 0)
                        {
                            int localX = px - x;
                            int localY = py - y;

                            if (localX < r && localY < r)
                                draw = (localX - r) * (localX - r) + (localY - r) * (localY - r) <= r2; // top-left
                            else if (localX >= width - r && localY < r)
                                draw = (localX - (width - r - 1)) * (localX - (width - r - 1)) + (localY - r) * (localY - r) <= r2; // top-right
                            else if (localX < r && localY >= height - r)
                                draw = (localX - r) * (localX - r) + (localY - (height - r - 1)) * (localY - (height - r - 1)) <= r2; // bottom-left
                            else if (localX >= width - r && localY >= height - r)
                                draw = (localX - (width - r - 1)) * (localX - (width - r - 1)) + (localY - (height - r - 1)) * (localY - (height - r - 1)) <= r2; // bottom-right
                        }

                        if (draw)
                        {
                            byte* pixel = bufferPtr + (py * pitch) + (px * _bytesPerPixel);
                            for (int b = 0; b < _bytesPerPixel; b++)
                                pixel[b] = colorPtr[b];
                        }
                    }
                }
            }
        }

        _dirtyRegions.Add(new Rectangle(x, y, width, height));
    }
    
    public void DrawText(int x, int y, string text, int fontSize, Color color)
    {
        FontRenderer.DrawText(this, x, y, text, fontSize);
    }
    
    public void DrawImage(int x, int y, BitmapImage image)
    {
        int bpp = _bitsPerPixel;
        int bytesPerPixel = _bytesPerPixel;
        int frameWidth = _width;
        int frameHeight = _height;
        int imgWidth = image.Width;
        int imgHeight = image.Height;
        byte[] srcPixels = image.PixelData;
        byte[]? alphaData = image.AlphaData;

        unsafe
        {
            fixed (byte* bufferPtr = _backBuffer)
            {
                for (int py = 0; py < imgHeight; py++)
                {
                    int destY = y + py;
                    if (destY < 0 || destY >= frameHeight) continue;

                    for (int px = 0; px < imgWidth; px++)
                    {
                        int destX = x + px;
                        if (destX < 0 || destX >= frameWidth) continue;

                        int srcIndex = (py * imgWidth + px);
                        byte r, g, b, a;

                        switch (bpp)
                        {
                            case 16:
                                ushort pixel = BitConverter.ToUInt16(srcPixels, srcIndex * 2);
                                Color.FromRgb565(pixel, out r, out g, out b);
                                a = alphaData?[srcIndex] ?? 255;
                                break;

                            case 32:
                                Color.FromBgra(srcPixels, srcIndex * 4, out r, out g, out b, out a);
                                break;

                            default:
                                throw new Exception("Unsupported bit depth: " + bpp);
                        }

                        if (a == 0) continue; // fully transparent

                        int destOffset = (destY * frameWidth + destX) * bytesPerPixel;
                        byte* dst = bufferPtr + destOffset;

                        if (a < 255)
                        {
                            // Alpha blend with existing framebuffer pixel
                            byte dstR = dst[2], dstG = dst[1], dstB = dst[0];

                            dst[2] = (byte)((r * a + dstR * (255 - a)) / 255);
                            dst[1] = (byte)((g * a + dstG * (255 - a)) / 255);
                            dst[0] = (byte)((b * a + dstB * (255 - a)) / 255);
                        }
                        else
                        {
                            // Fully opaque
                            byte[] colorBytes = GetRawColor(r, g, b);
                            for (int i = 0; i < colorBytes.Length; i++)
                                dst[i] = colorBytes[i];
                        }
                    }
                }
            }
        }

        _dirtyRegions.Add(new Rectangle(x, y, imgWidth, imgHeight));
    }
}