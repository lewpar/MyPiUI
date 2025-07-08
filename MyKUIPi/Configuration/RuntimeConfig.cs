using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyKUIPi.Configuration;

public class RuntimeConfig
{
    [JsonIgnore]
    public static RuntimeConfig? Instance;
    
    [JsonPropertyName("min_touch_x")]
    public int MinTouchX { get; set; }
    
    [JsonPropertyName("min_touch_y")]
    public int MinTouchY { get; set; }
    
    [JsonPropertyName("max_touch_x")]
    public int MaxTouchX { get; set; }
    
    [JsonPropertyName("max_touch_y")]
    public int MaxTouchY { get; set; }

    public static void Load(string configPath = "./runtime_config.json")
    {
        if (!File.Exists(configPath))
        {
            var contents = JsonSerializer.Serialize(new RuntimeConfig());
            File.WriteAllText(configPath, contents);
        }

        var json = File.ReadAllText(configPath);
        Instance = JsonSerializer.Deserialize<RuntimeConfig>(json);
    }

    public void Save(string configPath = "./runtime_config.json")
    {
        var contents = JsonSerializer.Serialize(this);
        File.WriteAllText(configPath, contents);
    }
}