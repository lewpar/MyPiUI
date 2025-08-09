using MyPiUI.Scene;
using MyPiUI.UI.Attributes;

namespace MyPiUI.Sample.Scenes;

public class TestScene : MyScene
{
    private bool _toggleState;

    public bool ToggleState
    {
        get => _toggleState;
        set => SetField(ref _toggleState, value);
    }
    
    private string? _toggleStateString;

    public string? ToggleStateString
    {
        get => _toggleStateString;
        set => SetField(ref _toggleStateString, value);
    }

    private bool _fontSizeState;

    private int _lastFontSizeUpdateSecond = -1;

    private string? _currentTime;
    public string? CurrentTime
    {
        get => _currentTime;
        set => SetField(ref _currentTime, value);
    }

    [ButtonHandler("SwitchToSecondaryScene")]
    private void SwitchToSecondaryScene()
    {
        MyEngine.Instance?.SceneManager.Push(new  SecondaryScene()
        {
            UI = "./SecondaryScene.xml"
        });
    }

    public override void Update(float deltaTimeMs)
    {
        var time = DateTime.Now;

        if (time.Second != _lastFontSizeUpdateSecond)
        {
            // Toggle font size only once per second when the second changes
            _fontSizeState = !_fontSizeState;

            _lastFontSizeUpdateSecond = time.Second;
        }
        
        ToggleStateString = ToggleState.ToString();
        CurrentTime = DateTime.Now.ToLongTimeString();
    
        base.Update(deltaTimeMs);
    }
}