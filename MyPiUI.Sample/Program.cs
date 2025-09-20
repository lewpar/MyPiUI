using MyPiUI.Drawing;
using MyPiUI.Input;
using MyPiUI.Primitives;
using MyPiUI.Sample.Scenes;

namespace MyPiUI.Sample;

class Program
{
    static void Main()
    {
        Console.WriteLine("Initializing MyPiUI Engine..");
        var engine = new MyEngine(new MyEngineOptions()
        {
            FrameBufferDevice = "/dev/fb0",
            
            TouchDevice = InputDeviceEnumerator.AutoDetectTouchDevice(),
            SwapTouchXAndY = true,
            InvertTouchX = true,
            InvertTouchY = false,
            
            HideConsoleCaret = true,
            ShowMetrics = true,
            ShowDebugUI = true,
            
            BackgroundColor = Color.Black,
            ForegroundColor = Color.White,
            
            RenderMode = RenderMode.FrameBuffer,
            
            RenderWidth = 480,
            RenderHeight = 320,
            
            HotReload = true,
            SkipTouchCalibration = false
        });
        
        engine.Initialize();
        
        Console.WriteLine("Loading TestScene..");
        engine.SceneManager.Push(new TestScene()
        {
            UI = "TestScene.xml"
        });
        
        bool isRunning = true;
        
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            isRunning = false;
            Console.Clear();
        };

        while (isRunning)
        {
            engine.Update();
            engine.Draw();
        }
        
        //engine.Dispose();
    }
}