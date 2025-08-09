namespace MyPiUI.Primitives;

public struct Size
{
    public int Width { get; init; }
    public int Height { get; init; }

    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }
}