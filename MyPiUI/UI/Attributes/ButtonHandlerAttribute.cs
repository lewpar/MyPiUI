using JetBrains.Annotations;

namespace MyPiUI.UI.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class ButtonHandlerAttribute : Attribute
{
    public string Name { get; init; }

    public ButtonHandlerAttribute(string name)
    {
        Name = name;
    }
}