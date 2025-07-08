using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using MyKUIPi.Scene;
using MyKUIPi.UI.Attributes;
using MyKUIPi.UI.Controls;
using MyKUIPi.UI.DataBinding;

namespace MyKUIPi.UI;

public class UIHandler
{
    public const string Namespace = "http://my.kuipi.com/ui";

    private static void SetParent(UIElement element, UIElement parent)
    {
        element.Parent = parent;

        foreach (var child in element.Children)
        {
            SetParent(child, element);
        }
    }

    private static void AutowireButton(MyScene scene, ButtonElement button)
    {
        var methods = scene.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var method = methods.FirstOrDefault(m =>
        {
            var methodAttribute = m.GetCustomAttribute<ButtonHandlerAttribute>();
            if (methodAttribute is null)
            {
                return false;
            }

            if (methodAttribute.Name != button.HandlerName)
            {
                return false;
            }

            return true;
        });

        if (method is null)
        {
            throw new Exception($"Method '{button.HandlerName}' not found in '{scene.GetType().FullName}'.");
        }
        
        button.Handler = (Action)method.CreateDelegate(typeof(Action), scene);
    }
    
    private static readonly Dictionary<TextAreaElement, object> TextAreaBindings = new();

    
    private static void OnBindableChanged<T>(T newValue)
    {
        foreach (var pair in TextAreaBindings)
        {
            if (pair.Value is BindableProperty<T> bindable && EqualityComparer<T>.Default.Equals(bindable.Value, newValue))
            {
                pair.Key.Text = newValue?.ToString() ?? "";
            }
        }
    }

    private static void AutowireTextArea(MyScene scene, TextAreaElement textArea)
    {
        if (string.IsNullOrWhiteSpace(textArea.Text))
        {
            return;
        }
        
        // Extract binding name from e.g. "{current_time}"
        string bindingName = textArea.Text.Trim('{', '}');

        var field = scene.GetType()
            .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(f =>
            {
                var attr = f.GetCustomAttribute<BindablePropertyAttribute>();
                return attr != null && attr.Name == bindingName;
            });

        if (field == null)
        {
            throw new Exception($"No BindableProperty with name '{bindingName}' found in '{scene.GetType().Name}'.");
        }

        var bindableObj = field.GetValue(scene);

        if (bindableObj == null)
        {
            throw new Exception($"Field '{bindingName}' is null.");
        }

        // Get current value and set it to the UI
        var bindableType = bindableObj.GetType();
        var valueProperty = bindableType.GetProperty("Value");
        var currentValue = valueProperty?.GetValue(bindableObj)?.ToString();

        if (currentValue != null)
        {
            textArea.Text = currentValue;
        }

        // Subscribe to changes
        var eventInfo = bindableType.GetEvent("ValueChanged");
        if (eventInfo != null)
        {
            var handlerType = eventInfo.EventHandlerType!;
            var method = typeof(UIHandler).GetMethod(nameof(OnBindableChanged), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(valueProperty!.PropertyType);

            var handlerDelegate = Delegate.CreateDelegate(handlerType, method);

            eventInfo.AddEventHandler(bindableObj, handlerDelegate);

            // Store textArea so handler knows where to update
            TextAreaBindings[textArea] = bindableObj;
        }
    }


    private static void Autowire(MyScene scene, UIElement element)
    {
        if (element is ButtonElement button)
        {
            if (!string.IsNullOrWhiteSpace(button.HandlerName))
            {
                AutowireButton(scene, button);   
            }
        }

        if (element is TextAreaElement textArea)
        {
            if (!string.IsNullOrWhiteSpace(textArea.Text) &&
                textArea.Text.StartsWith("{") &&
                textArea.Text.EndsWith("}"))
            {
                AutowireTextArea(scene, textArea);
            }
        }

        if (element is ImageElement image)
        {
            image.LoadImage();
        }
        
        foreach (var child in element.Children)
        {
            Autowire(scene, child);
        }
    }

    public static FrameElement Load(MyScene scene, string xmlPath)
    {
        if (!File.Exists(xmlPath))
        {
            throw new FileNotFoundException($"No UI exists at path '{xmlPath}'.");
        }
        
        var xmlSettings = new XmlReaderSettings();
        
        xmlSettings.ValidationType = ValidationType.Schema;
        xmlSettings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
        xmlSettings.Schemas.Add(Namespace, "./UI/ui.xsd");

        xmlSettings.ValidationEventHandler += (_, args) => throw new Exception(args.Message);
        
        var xmlStream = File.OpenRead(xmlPath);
        var xmlReader = XmlReader.Create(xmlStream, xmlSettings);
        var serializer = new XmlSerializer(typeof(FrameElement), Namespace);

        var frame = serializer.Deserialize(xmlReader) as FrameElement;
        if (frame is null)
        {
            throw new Exception($"Failed to deserialize frame element from path '{xmlPath}'.");
        }

        foreach (var child in frame.Children)
        {
            SetParent(child, frame);
            Autowire(scene, child);
        }

        return frame;
    }
}