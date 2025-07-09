using System.Xml.Serialization;
using MyKUIPi.Drawing;
using MyKUIPi.Primitives;

namespace MyKUIPi.UI.Controls;

public class StackPanelElement : UIElement
{
    private string? _bindableOrientation;
    [XmlAttribute("orientation")]
    public string? BindableOrientation
    {
        get => _bindableOrientation;
        set
        {
            _bindableOrientation = value;
            if (TryParseBindableInt(value, out var parsed))
                Orientation = parsed;
        }
    }

    [XmlIgnore]
    public int Orientation { get; set; }

    private string? _bindableGap;
    [XmlAttribute("gap")]
    public string? BindableGap
    {
        get => _bindableGap;
        set
        {
            _bindableGap = value;
            if (TryParseBindableInt(value, out var parsed))
                Gap = parsed;
        }
    }

    [XmlIgnore]
    public int Gap { get; set; }

    public override void Init()
    {
        int offsetX = X + Padding;
        int offsetY = Y + Padding;

        foreach (var child in Children)
        {
            child.X = offsetX;
            child.Y = offsetY;

            if (Orientation == (int)StackOrientation.Vertical)
            {
                offsetY += child.Height + Gap;
                child.Width = Width - Padding * 2;
            }
            else
            {
                offsetX += child.Width + Gap;
                child.Height = Height - Padding * 2;
            }

            child.Init();
        }
    }

    public override void Draw(DrawBuffer buffer)
    {
        int offsetX = X + Padding;
        int offsetY = Y + Padding;

        var panelBounds = new Rectangle(X, Y, Width, Height);
        buffer.SetClipRect(panelBounds);

        foreach (var child in Children)
        {
            child.X = offsetX;
            child.Y = offsetY;

            if (Orientation == (int)StackOrientation.Vertical)
            {
                offsetY += child.Height + Gap;
                child.Width = Width - Padding * 2;
            }
            else
            {
                offsetX += child.Width + Gap;
                child.Height = Height - Padding * 2;
            }

            child.Draw(buffer);
        }

        buffer.ClearClipRect();
    }
}
