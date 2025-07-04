namespace MyKUIPi.UI.DataBinding;

public class BindableProperty<T>
{
    private T _value;

    public event Action<T>? ValueChanged;

    public T Value
    {
        get => _value;
        set
        {
            if (!Equals(_value, value))
            {
                _value = value;
                ValueChanged?.Invoke(value);
            }
        }
    }

    public BindableProperty(T initialValue = default!)
    {
        _value = initialValue;
    }
}
