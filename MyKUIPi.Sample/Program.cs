using MyKUIPi.Input;
using MyKUIPi.Primitives;
using MyKUIPi.Sample.Scenes;

namespace MyKUIPi.Sample;

class Program
{
    static void Main()
    {
        var engine = new MyEngine(new MyEngineOptions()
        {
            FrameBufferDevice = "/dev/fb0",
            
            KeyboardDevice = InputDeviceEnumerator.AutoDetectKeyboardDevice(),
            MouseDevice = InputDeviceEnumerator.AutoDetectMouseDevice(),
            TouchDevice = InputDeviceEnumerator.AutoDetectTouchDevice(),
            MaxTouchX = 1452,
            MaxTouchY = 912,
            
            HideConsoleCaret = true,
            ShowMetrics = true,
            ShowDebugUI = true,
            
            BackgroundColor = new Color(220, 220, 220),
            ForegroundColor = Color.Black,
        });
        
        engine.Initialize();
        engine.SceneManager.Push(new TestScene()
        {
            UI = "./UI/TestScene.xml"
        });
        
        bool isRunning = true;
        
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            isRunning = false;
        };

        while (isRunning)
        {
            engine.Update();
        }
    }
}