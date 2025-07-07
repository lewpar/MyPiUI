using MyKUIPi.Drawing;
using MyKUIPi.Input;
using MyKUIPi.UI.Controls;

namespace MyKUIPi.Scene;

public class MyScene
{
    public InputManager? Input { get; set; }

    public string? UI { get; set; }
    public FrameElement? UIFrame { get; set; }

    public virtual void Draw(DrawBuffer buffer) { }
    public virtual void Update(float deltaTimeMs) { }
}