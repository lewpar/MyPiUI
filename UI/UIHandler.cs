using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using MyKUIPi.Scene;
using MyKUIPi.UI.Attributes;
using MyKUIPi.UI.Controls;

namespace MyKUIPi.UI;

public class UIHandler
{
    public const string NAMESPACE = "http://rawdraw.com/ui";

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

    private static void Autowire(MyScene scene, UIElement element)
    {
        if (element is ButtonElement)
        {
            var button = element as ButtonElement;
            if (button is not null &&
                !string.IsNullOrWhiteSpace(button.HandlerName))
            {
                AutowireButton(scene, button);   
            }
        }
        
        foreach (var child in element.Children)
        {
            Autowire(scene, child);
        }
    }

    public static FrameElement Load(MyScene scene, string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"No ui exists at path '{path}'.");
        }
        
        var xmlSettings = new XmlReaderSettings();
        
        xmlSettings.ValidationType = ValidationType.Schema;
        xmlSettings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
        xmlSettings.Schemas.Add(NAMESPACE, "./UI/ui.xsd");

        xmlSettings.ValidationEventHandler += (_, args) => throw new Exception(args.Message);
        
        var xmlStream = File.OpenRead(path);
        var xmlReader = XmlReader.Create(xmlStream, xmlSettings);
        var serializer = new XmlSerializer(typeof(FrameElement), NAMESPACE);

        var frame = serializer.Deserialize(xmlReader) as FrameElement;
        if (frame is null)
        {
            throw new Exception($"Failed to deserialize frame element from path '{path}'.");
        }

        foreach (var child in frame.Children)
        {
            SetParent(child, frame);
        }

        foreach (var child in frame.Children)
        {
            Autowire(scene, child);
        }

        return frame;
    }
}