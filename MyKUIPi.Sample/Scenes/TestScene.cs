using MyKUIPi.Scene;
using MyKUIPi.UI.Attributes;
using MyKUIPi.UI.DataBinding;

namespace MyKUIPi.Sample.Scenes;

public class TestScene : MyScene
{
    [ButtonHandler("open_other")]
    private void OpenOtherScene()
    {
        var sceneManager = SceneManager.Instance;
        if (sceneManager is null)
        {
            return;
        }
        
        sceneManager.Push(new OtherScene()
        {
            UI = "UI/OtherScene.xml"
        });
    }
    
    [ButtonHandler("open_settings")]
    private void OpenSettingsScene()
    {
        var sceneManager = SceneManager.Instance;
        if (sceneManager is null)
        {
            return;
        }
        
        sceneManager.Push(new SettingsScene()
        {
            UI = "UI/SettingsScene.xml"
        });
    }
 
    [BindableProperty("current_time")]
    private readonly BindableProperty<string> _currentTime = new BindableProperty<string>();
    
    [BindableProperty("font_size")]
    private readonly BindableProperty<int> _fontSize = new BindableProperty<int>();

    private bool _fontSizeState;

    private int _lastFontSizeUpdateSecond = -1;

    public override void Update(float deltaTimeMs)
    {
        var time = DateTime.Now;
        _currentTime.Value = time.ToString("HH:mm:ss");

        if (time.Second != _lastFontSizeUpdateSecond)
        {
            // Toggle font size only once per second when the second changes
            _fontSizeState = !_fontSizeState;
            _fontSize.Value = _fontSizeState ? 8 : 16;

            _lastFontSizeUpdateSecond = time.Second;
        }
    
        base.Update(deltaTimeMs);
    }
}