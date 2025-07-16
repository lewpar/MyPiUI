namespace MyPiUI.Primitives;

public struct Rectangle
{
    public int X { get; init; }
    public int Y { get; init; }
    
    public int Width { get; init; }
    public int Height { get; init; }

    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        
        Width = width;
        Height = height;
    }

    public bool ContainsPoint(int px, int py)
    {
        return px >= X && px < X + Width && py >= Y && py < Y + Height;
    }
}