using System.Xml.Serialization;
using MyPiUI.Drawing;
using MyPiUI.Input;
using MyPiUI.Primitives;

namespace MyPiUI.UI.Controls;

public class ButtonElement : UIElement
{
    [XmlAttribute("text")]
    public string? Text { get; set; }

    private string? _bindableFontSize;
    [XmlAttribute("font-size")]
    public string? BindableFontSize
    {
        get => _bindableFontSize;
        set
        {
            _bindableFontSize = value;
            if (TryParseBindableInt(value, out var parsed))
                FontSize = parsed;
        }
    }

    [XmlIgnore]
    public int FontSize { get; set; }

    private string? _bindableBorderSize;
    [XmlAttribute("border-size")]
    public string? BindableBorderSize
    {
        get => _bindableBorderSize;
        set
        {
            _bindableBorderSize = value;
            if (TryParseBindableInt(value, out var parsed))
                BorderSize = parsed;
        }
    }

    [XmlIgnore]
    public int BorderSize { get; set; } = 3;

    public Color BorderColor { get; set; }

    [XmlAttribute("border-color")]
    public string BorderColorHex
    {
        get => Color.ToHex(BorderColor);
        set => BorderColor = string.IsNullOrWhiteSpace(value) ? Color.Gray : Color.FromHex(value);
    }

    public Color? BackgroundHover { get; set; }

    [XmlAttribute("background-hover")]
    public string? BackgroundHoverHex
    {
        get => BackgroundHover is null ? null : Color.ToHex(BackgroundHover.Value);
        set => BackgroundHover = string.IsNullOrWhiteSpace(value) ? null : Color.FromHex(value);
    }

    private bool _currentTouchState;

    [XmlAttribute("handler")]
    public string? HandlerName { get; set; }

    [XmlIgnore]
    public Action? Handler { get; set; }

    private ImageElement? _image;
    
    private DateTime _timeSinceLastTouch;
    private int _delayBetweenTouchesMs;

    public ButtonElement()
    {
        _timeSinceLastTouch = DateTime.Now;
        _delayBetweenTouchesMs = 500;
    }

    public void OnTouch()
    {
        var now = DateTime.Now;
        var timeDelta = now - _timeSinceLastTouch;

        if (timeDelta.Milliseconds >= _delayBetweenTouchesMs)
        {
            Handler?.Invoke();
            _timeSinceLastTouch = DateTime.Now;
        }
    }

    public override void Init(MyGraphicsContext graphicsContext)
    {
        RecalculateBounds();
        
        if (Children.Count > 0)
        {
            var child = Children[0];
            if (child is ImageElement image)
            {
                _image = image;
            }
            
            base.Init(graphicsContext);
        }
    }

    private void RecalculateBounds()
    {
        Width = (Width > 0 ? Width : MeasureText(FontSize, Text ?? "")) + (Padding * 2);
        Height = (Height > 0 ? Height : FontSize) + (Padding * 2);
    }

    public override void Update(float deltaTimeMs)
    {
        _currentTouchState = InputManager.IsTouching(X, Y, Width, Height);

        if (_currentTouchState)
        {
            OnTouch();
        }

        base.Update(deltaTimeMs);
    }

    public override void Draw(DrawBuffer buffer)
    {
        buffer.SetClipRect(new Rectangle(X, Y, Width, Height));

        if (Background is not null &&
            !_currentTouchState)
        {
            buffer.FillRect(X, Y, Width, Height, Background.Value, 5);   
        }
        
        if (BackgroundHover is not null &&
            _currentTouchState)
        {
            buffer.FillRect(X, Y, Width, Height, BackgroundHover.Value, 5);   
        }

        if (_image is not null)
        {
            // 1: Initial position
            _image.X = X;
            _image.Y = Y;
            
            // 2: Centering
            _image.X += (Width / 2) - (_image.Width / 2);
            _image.Y += (Height / 2) - (_image.Height / 2);
            
            _image.Draw(buffer);
        }

        if (!string.IsNullOrWhiteSpace(Text))
        {
            var textWidth = MeasureText(FontSize, Text);
            var posX = X + (Width / 2) - (textWidth / 2);
            var posY = Y + (Height / 2) - (FontSize / 2);

            buffer.DrawText(posX, posY, Text, Foreground, FontSize);
        }

        buffer.ClearClipRect();
    }
}
