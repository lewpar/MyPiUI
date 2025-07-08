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

    public override void Update(float deltaTimeMs)
    {
        _currentTime.Value = DateTime.Now.ToString("HH:mm:ss");
        
        base.Update(deltaTimeMs);
    }
}