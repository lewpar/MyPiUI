using MyKUIPi.UI;

namespace MyKUIPi.Scene;

public class SceneManager
{
    public SceneManager? Instance { get; set; }
    
    public MyScene? CurrentScene => _scenes.Count > 0 ? _scenes.Peek() : null;

    private Stack<MyScene> _scenes;

    public SceneManager()
    {
        _scenes = new Stack<MyScene>();

        if (Instance is null)
        {
            Instance = this;
        }
    }

    public void Push(MyScene scene)
    {
        if (!string.IsNullOrWhiteSpace(scene.UI))
        {
            var uiFrame = UIHandler.Load(scene, scene.UI);
            scene.UIFrame = uiFrame;   
        }

        _scenes.Push(scene);
    }

    public void Pop()
    {
        _scenes.Pop();
    }
}