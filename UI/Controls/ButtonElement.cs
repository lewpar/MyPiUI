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
        set => Foreground = string.IsNullOrWhiteSpace(value) ? Color.Black : Color.FromHex(value);
    }
    
    public Color Background { get; set; }

    [XmlAttribute("background")]
    public string BackgroundHex
    {
        get => Color.ToHex(Background);
        set => Background = string.IsNullOrWhiteSpace(value) ? Color.White : Color.FromHex(value);
    }
    
    [XmlAttribute("x")]
    public int X { get; set; }
    
    [XmlAttribute("y")]
    public int Y { get; set; }
    
    [XmlAttribute("width")]
    public int Width { get; set; }
    
    [XmlAttribute("height")]
    public int Height { get; set; }
    
    [XmlAttribute("padding")]
    public int Padding { get; set; }

    private Rectangle? _bounds;
    
    private bool _currentTouchState;
    private bool _lastTouchState;

    [XmlIgnore]
    public Rectangle Bounds
    {
        get
        {
            if (_bounds is null)
            {
                _bounds = new Rectangle(X, Y, 
                    (Width > 0 ? Width : MeasureText(FontSize, Text ?? "")) + (Padding * 2), 
                    FontSize + (Padding * 2));
            }

            return _bounds.Value;
        }
    }

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

    public override void Update(float deltaTimeMs)
    {
        _currentTouchState = InputManager.IsTouching(Bounds);
        
        if (!_lastTouchState && _currentTouchState)
        {
            OnTouch();
        }
        
        _lastTouchState = _currentTouchState;
        
        base.Update(deltaTimeMs);
    }

    public override void Draw(FrameBuffer buffer)
    {
        if (_currentTouchState)
        {
            buffer.FillRect(X, Y, 
                (Width > 0 ? Width : MeasureText(FontSize, Text ?? "")) + (Padding * 2), 
                FontSize + (Padding * 2), Color.White);
            
            buffer.DrawRect(X, Y, 
                (Width > 0 ? Width : MeasureText(FontSize, Text ?? "")) + (Padding * 2), 
                FontSize + (Padding * 2), BorderSize, Color.White);
        }
        else
        {
            buffer.FillRect(X, Y, 
                (Width > 0 ? Width : MeasureText(FontSize, Text ?? "")) + (Padding * 2), 
                FontSize + (Padding * 2), Color.Gray);
            
            buffer.DrawRect(X, Y, 
                (Width > 0 ? Width : MeasureText(FontSize, Text ?? "")) + (Padding * 2), 
                FontSize + (Padding * 2), BorderSize, Color.Gray);   
        }

        if (!string.IsNullOrWhiteSpace(Text))
        {
            buffer.DrawText(X + Padding, Y + Padding, Text, Foreground, FontSize);   
        }
    }
}