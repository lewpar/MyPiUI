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
            
            HideConsoleCaret = true,
            ShowMetrics = false,
            ShowDebugUI = true,
            
            BackgroundColor = Color.Black,
            ForegroundColor = Color.White,
            
            RenderMode = RenderMode.Raylib,
            
            RenderWidth = 800,
            RenderHeight = 480,
            
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