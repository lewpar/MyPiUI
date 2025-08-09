using System.Diagnostics;

using MyPiUI.UI;

namespace MyPiUI.Scene;

public class SceneManager
{
    public static SceneManager? Instance { get; set; }
    
    public MyScene? CurrentScene => _scenes.Count > 0 ? _scenes.Peek() : null;
    private readonly Stack<MyScene> _scenes;
    private readonly SceneFileWatcher? _sceneWatcher;

    public SceneManager(MyEngineOptions myOptions)
    {
        _scenes = new Stack<MyScene>();

        if (myOptions.HotReload)
        {
            _sceneWatcher = new SceneFileWatcher();
            _sceneWatcher.SceneContentChanged += SceneWatcherOnSceneContentChanged;
        }

        if (Instance is null)
        {
            Instance = this;
        }
    }

    private void SceneWatcherOnSceneContentChanged(object? sender, EventArgs e)
    {
        Debug.Assert(MyEngine.Instance is not null);
        
        if (CurrentScene is null ||
            string.IsNullOrWhiteSpace(CurrentScene.UI))
        {
            return;
        }

        CurrentScene.UIFrame = MyUI.LoadUIElements(CurrentScene);
        CurrentScene.UIFrame.Init(MyEngine.Instance.GraphicsContext, MyEngine.Instance.Buffer);
    }

    public void Push(MyScene scene)
    {
        Debug.Assert(MyEngine.Instance is not null);
        
        if (!string.IsNullOrWhiteSpace(scene.UI))
        {
            MyEngine.Instance.Buffer.Clear();
            
            var uiFrame = MyUI.LoadUIElements(scene);
            scene.UIFrame = uiFrame;
            
            uiFrame.Init(MyEngine.Instance.GraphicsContext, MyEngine.Instance.Buffer);

            if (_sceneWatcher is not null)
            {
                _sceneWatcher.SetScene(scene);
            }
        }

        _scenes.Push(scene);
    }

    public void Pop()
    {
        _scenes.Pop();

        if (_sceneWatcher is not null &&
            _scenes.Count > 0)
        {
            _sceneWatcher.SetScene(_scenes.Peek());
        }
    }
}