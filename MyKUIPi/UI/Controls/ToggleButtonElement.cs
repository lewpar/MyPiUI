using System.Xml.Serialization;
using MyKUIPi.Drawing;
using MyKUIPi.Input;
using MyKUIPi.Primitives;

namespace MyKUIPi.UI.Controls;

public class ToggleButtonElement : UIElement
{
    private Color _fallbackFillColor;
    private Color _fallbackToggleInactiveColor;
    private Color _fallbackToggleActiveColor;

    private bool _currentTouchState;
    private bool _prevTouchState;
    
    private string? _bindableIsToggled;
    
    [XmlAttribute("is-toggled")]
    public string? BindableIsToggled
    {
        get => _bindableIsToggled;
        set
        {
            _bindableIsToggled = value;
            if (TryParseBindableBool(value, out var parsed))
                IsToggled = parsed;
        }
    }

    private bool _isToggled;

    public bool IsToggled
    {
        get => _isToggled;
        set => SetField(ref _isToggled, value);
    }

    public ToggleButtonElement()
    {
        _fallbackFillColor = Color.FromHex("404040");
        _fallbackToggleInactiveColor = Color.FromHex("606060");
        _fallbackToggleActiveColor = Color.FromHex("e0e0e0");
    }

    public override void Update(float deltaTimeMs)
    {
        _currentTouchState = InputManager.IsTouching(X, Y, Width, Height);

        if (_currentTouchState && !_prevTouchState)
        {
            IsToggled = !IsToggled;
        }
        
        _prevTouchState = _currentTouchState;
    }

    public override void Draw(DrawBuffer buffer)
    {
        buffer.FillRect(X, Y, Width, Height, Background ?? _fallbackFillColor, 5);

        if (!IsToggled)
        {
            buffer.FillRect(X + Padding, Y + Padding, Width / 2, Height - (Padding * 2), Background ?? _fallbackToggleInactiveColor, 5);   
        }
        else
        {
            buffer.FillRect(X + (Width / 2), Y + Padding, Width / 2, Height - (Padding * 2), Background ?? _fallbackToggleActiveColor, 5);
        }
    }
}