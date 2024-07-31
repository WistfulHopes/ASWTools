using System.Collections.Generic;

namespace ADVScriptEditor.ADVScript;

public enum AdvArg
{
    Int,
    String,
}

public class AdvConfig
{
    public Dictionary<string, List<AdvArg>>? Commands { get; set; }    
    private static AdvConfig? _instance = null;
    public static AdvConfig Instance => _instance ??= new AdvConfig();
}