namespace BBScript.Config;

public class BBSConfig
{
    public List<string>? JumpTableEntries { get; set; }
    public Dictionary<string, int>? Variables { get; set; }
    public Dictionary<string, Dictionary<string, int>?>? Enums { get; set; }
    public Dictionary<int, Instruction>? Instructions { get; set; }
    
    private static BBSConfig? _instance = null;
    public static BBSConfig Instance => _instance ??= new BBSConfig();
}