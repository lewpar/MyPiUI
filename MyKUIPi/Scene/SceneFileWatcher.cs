using MyKUIPi.UI;

namespace MyKUIPi.Scene;

using System;
using System.IO;

public class SceneFileWatcher
{
    private FileSystemWatcher? _watcher;
    private string? _filePath;
    private string? _fileName;

    public event EventHandler? SceneContentChanged;

    public void SetScene(MyScene scene)
    {
        var path = MyUI.GetXmlPath(scene);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File to watch not found", scene.UI);
        }

        _filePath = path;
        _fileName = Path.GetFileName(path);
        
        Console.WriteLine($"Listening for file changes to '{_filePath}'.");

        string directory = Path.GetDirectoryName(_filePath)!;

        _watcher = new FileSystemWatcher(directory)
        {
            Filter = _fileName,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnSceneContentChanged;
    }

    private void OnSceneContentChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed)
        {
            return;
        }
        
        if (IsTargetFile(e.FullPath))
        {
            SceneContentChanged?.Invoke(null, EventArgs.Empty);
            Console.WriteLine(e.FullPath);
        }
    }

    private bool IsTargetFile(string path)
    {
        return string.Equals(Path.GetFullPath(path), _filePath, StringComparison.OrdinalIgnoreCase);
    }
}
