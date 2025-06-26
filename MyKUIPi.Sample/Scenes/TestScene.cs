using MyKUIPi.Scene;
using MyKUIPi.UI.Attributes;

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
}