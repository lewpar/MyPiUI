using System.Security.Cryptography;

using MyKUIPi.UI;

namespace MyKUIPi.Scene;

public class SceneManager
{
    public static SceneManager? Instance { get; set; }
    
    public MyScene? CurrentScene => _scenes.Count > 0 ? _scenes.Peek() : null;
    private Stack<MyScene> _scenes;
    private SceneFileWatcher? _sceneWatcher;

    public SceneManager(MyEngineOptions myOptions)
    {
        _scenes = new Stack<MyScene>();

        if (myOptions.HotReload)
        {
            _sceneWatcher = new SceneFileWatcher();
            _sceneWatcher.SceneContentChanged += (_, _) =>
            {
                if (CurrentScene is null ||
                    string.IsNullOrWhiteSpace(CurrentScene.UI))
                {
                    return;
                }

                CurrentScene.UIFrame = UIHandler.Load(CurrentScene, CurrentScene.UI);
                CurrentScene.UIFrame.Init();
            };   
        }

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
            
            uiFrame.Init();

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
    }
    
    private static string ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        byte[] hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}