namespace MyPiUI.UI.Attributes;

public class ButtonHandlerAttribute : Attribute
{
    public string Name { get; init; }

    public ButtonHandlerAttribute(string name)
    {
        Name = name;
    }
}