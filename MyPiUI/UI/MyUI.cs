using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using MyPiUI.Scene;
using MyPiUI.UI.Attributes;
using MyPiUI.UI.Controls;

namespace MyPiUI.UI;

public class MyUI
{
    public const string Namespace = "http://my.kuipi.com/ui";

    public static string GetXmlPath(MyScene scene)
    {
        var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new Exception("Failed to get path to xml resources.");
        }
        
#if DEBUG
        var directoryPath = FindProjectDirectory(rootPath);
        if (directoryPath is not null)
        {
            rootPath = directoryPath;
        }
#endif

        var path = Path.Combine(rootPath, "UI", scene.UI);

        return path;
    }
    
    private static string? FindProjectDirectory(string? startPath = null)
    {
        var dir = new DirectoryInfo(startPath ?? AppContext.BaseDirectory);

        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Any())
                return dir.FullName;

            dir = dir.Parent;
        }

        return null;
    }
    
    private static void SetParentElement(UIElement element, UIElement parent)
    {
        element.Parent = parent;

        foreach (var child in element.Children)
        {
            SetParentElement(child, element);
        }
    }

    private static void SetupButtonHandlers(MyScene scene, ButtonElement button)
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

    private static void SetupDatabinding(MyScene scene, UIElement element)
    {
        if (scene is not INotifyPropertyChanged sceneNotifier)
            throw new Exception("Scene must implement INotifyPropertyChanged.");

        if (element is not INotifyPropertyChanged uiNotifier)
            throw new Exception("UIElement must implement INotifyPropertyChanged.");

        var elementProps = element.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var uiProp in elementProps)
        {
            // Step 1: Ignore non-string properties (not bindable)
            if (uiProp.PropertyType != typeof(string) || !uiProp.CanRead || !uiProp.CanWrite)
            {
                continue;
            }

            // Step 2: Ignore empty or string that do not start and end with { } (not a binding)
            var rawValue = uiProp.GetValue(element) as string;
            if (string.IsNullOrWhiteSpace(rawValue) || !rawValue.StartsWith("{") || !rawValue.EndsWith("}"))
            {
                continue;
            }

            // Step 3: Extract the property name from the binding
            var bindingName = rawValue.Trim('{', '}');

            // Step 4: Fetch the property inside the scene from the binding name
            var sceneProp = scene.GetType().GetProperty(bindingName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Step 5: Invalid property name in users current scene.
            if (sceneProp is null || !sceneProp.CanRead || !sceneProp.CanWrite)
            {
                throw new Exception($"Property '{bindingName}' not found in scene.");
            }
            
            // Step 6: Remove the Bindable prefix from the UI Element property name
            var uiPropertyNameActual = uiProp.Name.StartsWith("Bindable")
                ? uiProp.Name.Substring("Bindable".Length)
                : uiProp.Name;
            
            // Step 7: Fetch the property inside the UI Element
            var uiPropActual = element.GetType().GetProperty(uiPropertyNameActual,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            // Step 8: Invalid property name in UI Element
            if (uiPropActual is null || !uiPropActual.CanRead || !uiPropActual.CanWrite)
            {
                throw new Exception($"Failed to find underlying actual property '{bindingName}'.");
            }

            // Step 9: Setup subscriber for scene -> UI changes
            sceneNotifier.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == bindingName)
                {
                    var newVal = sceneProp.GetValue(scene)?.ToString();
                    uiProp.SetValue(element, newVal);
                }
            };

            // Step 10: Setup subscriber for UI -> scene changes
            uiNotifier.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == uiPropertyNameActual)
                {
                    var newVal = uiPropActual.GetValue(element);
                    sceneProp.SetValue(scene, newVal);
                }
            };
        }
    }

    private static void InitializeUIElement(MyScene scene, UIElement element)
    {
        if (element is ButtonElement button)
        {
            if (!string.IsNullOrWhiteSpace(button.HandlerName))
            {
                SetupButtonHandlers(scene, button);   
            }
        }

        SetupDatabinding(scene, element);

        if (element is ImageElement image)
        {
            image.LoadImage();
        }
        
        foreach (var child in element.Children)
        {
            InitializeUIElement(scene, child);
        }
    }

    public static string LoadXmlFromPath(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"No XML file exists at path '{path}'.");
        }

        var xml = File.ReadAllText(path);
        
        return xml;
    }

    public static FrameElement LoadUIElements(MyScene scene)
    {
        var path = GetXmlPath(scene);
        var xml = LoadXmlFromPath(path);
        using var xmlMemoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        
        var xmlSettings = new XmlReaderSettings();
        
        xmlSettings.ValidationType = ValidationType.Schema;
        xmlSettings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
        xmlSettings.Schemas.Add(Namespace, "./UI/ui.xsd");

        xmlSettings.ValidationEventHandler += (_, args) => throw new Exception(args.Message);

        using var xmlReader = XmlReader.Create(xmlMemoryStream, xmlSettings);
        var serializer = new XmlSerializer(typeof(FrameElement), Namespace);

        var frame = serializer.Deserialize(xmlReader) as FrameElement;
        if (frame is null)
        {
            throw new Exception($"Failed to deserialize frame element from path '{path}'.");
        }

        foreach (var child in frame.Children)
        {
            SetParentElement(child, frame);
            InitializeUIElement(scene, child);
        }

        return frame;
    }
}