using MyPiUI.Scene;
using MyPiUI.UI.Attributes;

namespace MyPiUI.Sample.Scenes;

public class SecondaryScene : MyScene
{
    [ButtonHandler("SwitchToPrimaryScene")]
    private void SwitchToPrimaryScene()
    {
        MyEngine.Instance?.SceneManager.Pop();
    }
}