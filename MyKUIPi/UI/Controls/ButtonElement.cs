using System.Diagnostics;
using System.Xml.Serialization;

using MyKUIPi.Drawing;
using MyKUIPi.Input;
using MyKUIPi.Primitives;

namespace MyKUIPi.UI.Controls;

public class ButtonElement : UIElement
{
    [XmlAttribute("text")]
    public string? Text { get; set; }
    
    [XmlAttribute("font-size")]
    public int FontSize { get; set; }

    [XmlAttribute("border-size")]
    public int BorderSize { get; set; } = 3;

    public Color BorderColor { get; set; }
    
    [XmlAttribute("border-color")]
    public string BorderColorHex
    {
        get => Color.ToHex(BorderColor);
        set => BorderColor = string.IsNullOrWhiteSpace(value) ? Color.Gray : Color.FromHex(value);
    }
    
    public Color Foreground { get; set; }

    [XmlAttribute("foreground")]
    public string ForegroundHex
    {
        get => Color.ToHex(Foreground);
        set => Foreground = string.IsNullOrWhiteSpace(value) ? Color.White : Color.FromHex(value);
    }
    
    public Color Background { get; set; }

    [XmlAttribute("background")]
    public string BackgroundHex
    {
        get => Color.ToHex(Background);
        set => Background = string.IsNullOrWhiteSpace(value) ? Color.DodgerBlue : Color.FromHex(value);
    }
    
    public Color BackgroundHover { get; set; }

    [XmlAttribute("background-hover")]
    public string BackgroundHoverHex
    {
        get => Color.ToHex(BackgroundHover);
        set => BackgroundHover = string.IsNullOrWhiteSpace(value) ? Color.SkyBlue : Color.FromHex(value);
    }
    
    private bool _currentTouchState;
    private bool _lastTouchState;

    [XmlAttribute("handler")]
    public string? HandlerName { get; set; }
    
    [XmlIgnore]
    public Action? Handler { get; set; }

    public void OnTouch()
    {
        if (Handler is null)
        {
            return;
        }

        Handler.Invoke();
    }

    public override void Init()
    {
        Width = (Width > 0 ? Width : MeasureText(FontSize, Text ?? "")) + (Padding * 2);
        Height = FontSize + (Padding * 2);
    }

    public override void Update(float deltaTimeMs)
    {
        _currentTouchState = InputManager.IsTouching(X, Y, Width, Height);
        
        if (_lastTouchState && !_currentTouchState)
        {
            OnTouch();
        }
        
        _lastTouchState = _currentTouchState;
        
        base.Update(deltaTimeMs);
    }

    public override void Draw(FrameBuffer buffer)
    {
        buffer.FillRect(X, Y, Width, Height, _currentTouchState ? BackgroundHover : Background);

        if (!string.IsNullOrWhiteSpace(Text))
        {
            buffer.DrawText(X + Padding, Y + Padding, Text, Foreground, FontSize);   
        }
    }
}