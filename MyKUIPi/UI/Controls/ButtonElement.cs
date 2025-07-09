using System.Xml.Serialization;

using MyKUIPi.Drawing;
using MyKUIPi.Input;
using MyKUIPi.Primitives;

namespace MyKUIPi.UI.Controls;

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

    public Color BackgroundHover { get; set; }

    [XmlAttribute("background-hover")]
    public string BackgroundHoverHex
    {
        get => Color.ToHex(BackgroundHover);
        set => BackgroundHover = string.IsNullOrWhiteSpace(value) ? Color.SkyBlue : Color.FromHex(value);
    }

    private bool _currentTouchState;
    private bool _wasTouchingScreenLastFrame;

    [XmlAttribute("handler")]
    public string? HandlerName { get; set; }

    [XmlIgnore]
    public Action? Handler { get; set; }

    private ImageElement? _image;

    public void OnTouch()
    {
        Handler?.Invoke();
    }

    public override void Init()
    {
        Width = (Width > 0 ? Width : MeasureText(FontSize, Text ?? "")) + (Padding * 2);
        Height = (Height > 0 ? Height : FontSize) + (Padding * 2);

        if (Children.Count > 0 && Children[0] is ImageElement image)
        {
            image.X = X + (Width / 2) - (image.Width / 2);
            image.Y = Y + (Height / 2) - (image.Height / 2);
            _image = image;
        }
    }

    public override void Update(float deltaTimeMs)
    {
        bool isScreenTouched = InputManager.IsTouching();
        _currentTouchState = InputManager.IsTouching(X, Y, Width, Height);

        if (!_wasTouchingScreenLastFrame && _currentTouchState)
        {
            OnTouch();
        }

        _wasTouchingScreenLastFrame = isScreenTouched;

        base.Update(deltaTimeMs);
    }

    public override void Draw(DrawBuffer buffer)
    {
        buffer.SetClipRect(new Rectangle(X, Y, Width, Height));
        buffer.FillRect(X, Y, Width, Height, _currentTouchState ? BackgroundHover : Background, 5);

        _image?.Draw(buffer);

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
