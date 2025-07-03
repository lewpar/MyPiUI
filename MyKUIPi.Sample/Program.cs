using MyKUIPi.Input;
using MyKUIPi.Primitives;
using MyKUIPi.Sample.Scenes;

namespace MyKUIPi.Sample;

class Program
{
    static void Main()
    {
        Console.WriteLine("Initializing MyKUIPi Engine..");
        var engine = new MyEngine(new MyEngineOptions()
        {
            FrameBufferDevice = "/dev/fb0",
            
            TouchDevice = InputDeviceEnumerator.AutoDetectTouchDevice(),
            
            HideConsoleCaret = true,
            ShowMetrics = true,
            ShowDebugUI = true,
            
            BackgroundColor = Color.Black,
            ForegroundColor = Color.White,
        });
        
        engine.Initialize();
        engine.CalibrateTouch();
        
        Console.WriteLine("Loading TestScene..");
        engine.SceneManager.Push(new TestScene()
        {
            UI = "./UI/TestScene.xml"
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
    }
}