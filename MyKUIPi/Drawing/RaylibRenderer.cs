namespace MyKUIPi.Drawing;

using System;

using Raylib_cs;

public class RaylibRenderer : IDisposable
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _bpp; // bytes per pixel (e.g. 4 for 32-bit)
    private readonly Texture2D _texture;
    private readonly Image _image;
    private bool _disposed;

    public RaylibRenderer(int width, int height, int bpp)
    {
        unsafe
        {
            _width = width;
            _height = height;
            _bpp = bpp;

            // Init raylib window and renderer
            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
            Raylib.InitWindow(_width, _height, "Framebuffer Viewer");
            Raylib.SetTargetFPS(60);

            // Create empty image and texture
            _image = new Image
            {
                Data = Raylib.MemAlloc((uint)(_width * _height * _bpp)),
                Width = _width,
                Height = _height,
                Mipmaps = 1,
                Format = PixelFormat.UncompressedR8G8B8A8 // 32-bit RGBA
            };

            _texture = Raylib.LoadTextureFromImage(_image);
        }
    }

    public void Draw(byte[] framebuffer)
    {
        if (framebuffer.Length != _width * _height * _bpp)
            throw new ArgumentException("Framebuffer size does not match dimensions.");

        // Update root ui frame when the screen resizes
        if (Raylib.IsWindowResized())
        {
            var uiFrame = MyEngine.Instance?.SceneManager?.CurrentScene?.UIFrame ?? null;
            if (uiFrame is not null)
            {
                uiFrame.Height = Raylib.GetRenderHeight();
                uiFrame.Width = Raylib.GetRenderWidth();
                uiFrame.Init();   
            }
        }

        if (Raylib.WindowShouldClose())
        {
            Raylib.CloseWindow();
        }

        unsafe
        {
            fixed (byte* fbPtr = framebuffer)
            {
                Raylib.UpdateTexture(_texture, (void*)fbPtr);
            }
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawTexture(_texture, 0, 0, Color.White);
        Raylib.EndDrawing();
    }

    public bool ShouldClose() => Raylib.WindowShouldClose();

    public void Dispose()
    {
        unsafe
        {
            if (_disposed) return;

            Raylib.UnloadTexture(_texture);
            Raylib.MemFree(_image.Data);
            Raylib.CloseWindow();

            _disposed = true;
        }
    }
}

