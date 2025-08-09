using System.Xml.Serialization;
using MyPiUI.Drawing;
using MyPiUI.Drawing.Buffers;
using MyPiUI.Input;
using MyPiUI.Primitives;

namespace MyPiUI.UI.Controls;

public class ToggleButtonElement : UIElement
{
    private Color _fallbackFillColor;
    private Color _fallbackToggleInactiveColor;
    private Color _fallbackToggleActiveColor;

    private bool _currentTouchState;
    private bool _prevTouchState;
    
    private string? _bindableIsToggled;
    
    private DateTime _timeSinceLastTouch;
    private int _delayBetweenTouchesMs;
    
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

        _timeSinceLastTouch = DateTime.Now;
        _delayBetweenTouchesMs = 250;
    }

    public override void Update(float deltaTimeMs)
    {
        _currentTouchState = InputManager.IsTouching(X, Y, Width, Height);
        
        if (_currentTouchState && !_prevTouchState)
        {
            var now = DateTime.Now;
            var timeDelta = now - _timeSinceLastTouch;

            if (timeDelta.Milliseconds >= _delayBetweenTouchesMs)
            {
                IsToggled = !IsToggled;
                _timeSinceLastTouch =  DateTime.Now;   
            }
        }
        
        _prevTouchState = _currentTouchState;
    }

    public override void Draw(IDrawBuffer buffer)
    {
        buffer.FillRect(new Rectangle(X, Y, Width, Height), Background ?? _fallbackFillColor);

        if (!IsToggled)
        {
            buffer.FillRect(new Rectangle(X + Padding, Y + Padding, Width / 2, Height - (Padding * 2)), Background ?? _fallbackToggleInactiveColor);   
        }
        else
        {
            buffer.FillRect(new Rectangle(X + (Width / 2), Y + Padding, Width / 2, Height - (Padding * 2)), Background ?? _fallbackToggleActiveColor);
        }
    }
}