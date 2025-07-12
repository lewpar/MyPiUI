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
            {
                Orientation = parsed;
            }
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
            {
                Gap = parsed;
            }
        }
    }

    [XmlIgnore]
    public int Gap { get; set; }

    private void UpdateStackPanelBounds()
    {
        int primarySize = 0;
        int secondarySize = 0;

        bool isVertical = Orientation == (int)StackOrientation.Vertical;

        foreach (var child in Children)
        {
            if (isVertical)
            {
                primarySize += child.Height;
                secondarySize = Math.Max(secondarySize, child.Width);
            }
            else
            {
                primarySize += child.Width;
                secondarySize = Math.Max(secondarySize, child.Height);
            }
        }

        if (Children.Count > 1)
            primarySize += Gap * (Children.Count - 1);

        primarySize += Padding * 2;
        secondarySize += Padding * 2;

        if (isVertical)
        {
            Width = secondarySize;
            Height = primarySize;
        }
        else
        {
            Width = primarySize;
            Height = secondarySize;
        }
    }

    private void UpdateChildPositions()
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
        }
    }

    public override void Draw(DrawBuffer buffer)
    {
        UpdateStackPanelBounds();
        UpdateChildPositions();
        
        var panelBounds = new Rectangle(X, Y, Width, Height);
        buffer.SetClipRect(panelBounds);

        foreach (var child in Children)
        {
            child.Draw(buffer);
        }

        buffer.ClearClipRect();
    }
}
