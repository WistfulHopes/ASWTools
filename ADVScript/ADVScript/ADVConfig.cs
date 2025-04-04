using System.Text.Json.Serialization;

namespace ADVScript.ADVScript;

[JsonConverter(typeof(JsonStringEnumConverter<ADVArg>))]
public enum ADVArg
{
    Int,
    String,
}

public class ADVConfig
{
    public Dictionary<string, List<ADVArg>>? Commands { get; set; }    
    private static ADVConfig? _instance;
    public static ADVConfig Instance => _instance ??= new ADVConfig();
}

[JsonSerializable(typeof(ADVConfig))]
internal partial class ADVConfigSourceGenerationContext : JsonSerializerContext
{
}
