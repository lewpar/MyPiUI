namespace MyKUIPi.UI.Attributes;

public class BindablePropertyAttribute : Attribute
{
    public string Name { get; init; }

    public BindablePropertyAttribute(string name)
    {
        Name = name;
    }
}