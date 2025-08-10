using System.ComponentModel;
using System.Runtime.CompilerServices;

using MyPiUI.Drawing.Buffers;
using MyPiUI.Input;
using MyPiUI.UI.Controls;

namespace MyPiUI.Scene;

public class MyScene : INotifyPropertyChanged
{
    public InputManager? Input { get; set; }

    public required string UI { get; init; }
    public FrameElement? UIFrame { get; set; }

    public virtual void Init() { }
    public virtual void Draw(IDrawBuffer buffer) { }
    public virtual void Update(float deltaTimeMs) { }
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}