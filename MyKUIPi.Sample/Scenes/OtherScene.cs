using MyKUIPi.Scene;
using MyKUIPi.UI.Attributes;

namespace MyKUIPi.Sample.Scenes;

public class OtherScene : MyScene
{
    [ButtonHandler("open_test")]
    private void OpenTestScene()
    {
        var sceneManager = SceneManager.Instance;
        if (sceneManager is null)
        {
            return;
        }
        
        sceneManager.Pop();
    }
}