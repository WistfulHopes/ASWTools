using System.Text.Json;
using System.Text.Json.Serialization;
using BBScript.Compiler;
using BBScript.Config;
using BBScript.Decompiler;
using CommandLine;

namespace BBScript;

abstract class Program
{
    [Verb("compile", HelpText = "Compiles a source BBScript file to binary.")]
    private class CompilerVerbs
    {
        [Option('c', "config", HelpText = "Config file.")]
        public string? Config { get; set; }
        
        [Option('i', "input", Required = true, HelpText = "Input file.")]
        public string? Input { get; set; }

        [Option('o', "output", HelpText = "Output file.")]
        public string? Output { get; set; }
    }

    [Verb("decompile", HelpText = "Decompiles a binary BBScript file to text.")]
    private class DecompilerVerbs
    {
        [Option('c', "config", HelpText = "Config file.")]
        public string? Config { get; set; }
        
        [Option('i', "input", Required = true, HelpText = "Input file.")]
        public string? Input { get; set; }

        [Option('o', "output", HelpText = "Output file.")]
        public string? Output { get; set; }
    }

    static void Main(string[] args)
    {
        var p = CommandLine.Parser.Default.ParseArguments<CompilerVerbs, DecompilerVerbs>(args);

        p.WithParsed<CompilerVerbs>(Compile).WithParsed<DecompilerVerbs>(Decompile);
    }

    static void Compile(CompilerVerbs verbs)
    {
        if (!File.Exists(verbs.Config)) return;
        if (!File.Exists(verbs.Input)) return;
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        var config = JsonSerializer.Deserialize<BBSConfig>(File.ReadAllText(verbs.Config), options);

        if (config == null) return;
        
        BBSConfig.Instance.JumpTableEntries = config.JumpTableEntries;
        BBSConfig.Instance.Variables = config.Variables;
        BBSConfig.Instance.Enums = config.Enums;
        BBSConfig.Instance.Instructions = config.Instructions;
        
        var compiler = new BBSCompiler();
        var output = compiler.Compile(File.ReadAllText(verbs.Input));
        
        File.WriteAllBytes(verbs.Output!, output);
    }

    static void Decompile(DecompilerVerbs verbs)
    {
        if (!File.Exists(verbs.Config)) return;
        if (!File.Exists(verbs.Input)) return;
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        var config = JsonSerializer.Deserialize<BBSConfig>(File.ReadAllText(verbs.Config), options);

        if (config == null) return;
        
        BBSConfig.Instance.JumpTableEntries = config.JumpTableEntries;
        BBSConfig.Instance.Variables = config.Variables;
        BBSConfig.Instance.Enums = config.Enums;
        BBSConfig.Instance.Instructions = config.Instructions;
        
        var output = BBSDecompiler.Decompile(File.ReadAllBytes(verbs.Input));
        
        File.WriteAllText(verbs.Output!, output);
    }
}