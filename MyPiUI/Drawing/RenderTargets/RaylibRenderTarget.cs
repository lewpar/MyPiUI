using Raylib_cs;

namespace MyPiUI.Drawing.RenderTargets;

public class RaylibRenderTarget : IRenderTarget, IDisposable
{
    private readonly Texture2D _texture;
    private readonly Image _image;
    
    private readonly Color? _backgroundColor;

    private readonly int _expectedBufferSize;
    
    public RaylibRenderTarget(int width, int height, int bpp = 32)
    {
        unsafe
        {
            var bytesPerPixel = bpp / 8;
            var bufferSize = width * height * bytesPerPixel;
            
            _expectedBufferSize = bufferSize;

            PixelFormat pixelFormat = PixelFormat.UncompressedR8G8B8A8;

            Raylib.InitWindow(width, height, $"Framebuffer Viewer ({width}x{height})");
            Raylib.SetTargetFPS(60);
            Raylib.SetWindowMinSize(width, height);
            Raylib.SetWindowMaxSize(width, height);

            _image = new Image
            {
                Data = Raylib.MemAlloc((uint)bufferSize),
                Width = width,
                Height = height,
                Mipmaps = 1,
                Format = pixelFormat
            };

            _texture = Raylib.LoadTextureFromImage(_image);

            var myOptions = MyEngine.Instance?.MyOptions;
            if (myOptions is not null)
            {
                _backgroundColor = new Color()
                {
                    R = myOptions.BackgroundColor.R,
                    G = myOptions.BackgroundColor.G,
                    B = myOptions.BackgroundColor.B,
                };
            }
        }
    }
    
    public void SwapBuffer(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length != _expectedBufferSize)
        {
            throw new Exception($"Invalid buffer size. Expected size of '{_expectedBufferSize}' got '{buffer.Length}'.");
        }
        
        if (Raylib.WindowShouldClose())
        {
            Raylib.CloseWindow();
        }

        Raylib.UpdateTexture(_texture, buffer);

        Raylib.BeginDrawing();
        Raylib.ClearBackground(_backgroundColor ?? Color.Black);
        Raylib.DrawTexture(_texture, 0, 0, Color.White);
        Raylib.EndDrawing();
    }

    public MyGraphicsContext CreateGraphicsContext()
    {
        var pixelFormat = GetPixelFormat();
        
        return new MyGraphicsContext()
        {
            PixelFormat = pixelFormat,
            BitsPerPixel = pixelFormat.GetBitsPerPixel(),
            Width = _image.Width,
            Height = _image.Height,
        };
    }

    private MyPixelFormat GetPixelFormat()
    {
        return MyPixelFormat.R8G8B8A8;
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