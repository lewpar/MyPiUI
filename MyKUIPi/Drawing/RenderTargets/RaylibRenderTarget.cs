using Raylib_cs;

namespace MyKUIPi.Drawing.RenderTargets;

public class RaylibRenderTarget : IRenderTarget, IDisposable
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _bpp; // bytes per pixel (e.g. 4 for 32-bit)
    private readonly Texture2D _texture;
    private readonly Image _image;
    
    public RaylibRenderTarget(int width, int height, int bpp = 32)
    {
        unsafe
        {
            _width = width;
            _height = height;
            _bpp = 32;

            PixelFormat? pixelFormat;

            switch (_bpp)
            {
                case 16:
                    pixelFormat = PixelFormat.UncompressedR5G6B5;
                    break;
                
                case 32:
                    pixelFormat = PixelFormat.UncompressedR8G8B8A8;
                    break;
                
                default:
                    throw new Exception(
                        "Unsupported bit depth for raylib render target. Only 16-bit and 32-bit colors are supported.");
            }

            Raylib.InitWindow(_width, _height, $"Framebuffer Viewer ({width}x{height})");
            Raylib.SetTargetFPS(60);
            Raylib.SetWindowMinSize(width, height);
            Raylib.SetWindowMaxSize(width, height);

            _image = new Image
            {
                Data = Raylib.MemAlloc((uint)(_width * _height * _bpp)),
                Width = _width,
                Height = _height,
                Mipmaps = 1,
                Format = pixelFormat.Value
            };

            _texture = Raylib.LoadTextureFromImage(_image);
        }
    }
    
    public void SwapBuffer(byte[] buffer)
    {
        if (buffer.Length != _width * _height * _bpp)
            throw new ArgumentException("Framebuffer size does not match dimensions.");

        if (Raylib.WindowShouldClose())
        {
            Raylib.CloseWindow();
        }

        unsafe
        {
            fixed (byte* bufferPtr = buffer)
            {
                Raylib.UpdateTexture(_texture, (void*)bufferPtr);
            }
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawTexture(_texture, 0, 0, Color.White);
        Raylib.EndDrawing();
    }
    
    public void Dispose()
    {
        unsafe
        {
            Raylib.UnloadTexture(_texture);
            Raylib.MemFree(_image.Data);
            Raylib.CloseWindow();
        }
    }
}