using MyKUIPi.Scene;
using MyKUIPi.UI.Attributes;

namespace MyKUIPi.Sample.Scenes;

public class SettingsScene : MyScene
{
    [ButtonHandler("close_settings")]
    private void CloseSettingsScene()
    {
        var sceneManager = SceneManager.Instance;
        if (sceneManager is null)
        {
            return;
        }
        
        sceneManager.Pop();
    }
}