using System.Text.Json;
using System.Text.RegularExpressions;
using ADVScript.ADVScript;
using CommandLine;

namespace ADVScript;

internal partial class Program
{
    [Verb("compile", HelpText = "Compiles a source ADVScript file to binary.")]
    private class CompilerVerbs
    {
        [Option('c', "config", HelpText = "Config file.")]
        public string? Config { get; set; }
        
        [Option('i', "input", Required = true, HelpText = "Input file.")]
        public string? Input { get; set; }

        [Option('o', "output", HelpText = "Output file.")]
        public string? Output { get; set; }
    }

    [Verb("decompile", HelpText = "Decompiles a binary ADVScript file to text.")]
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
        var p = Parser.Default.ParseArguments<CompilerVerbs, DecompilerVerbs>(args);

        p.WithParsed<CompilerVerbs>(Compile).WithParsed<DecompilerVerbs>(Decompile);
    }

    static void Compile(CompilerVerbs verbs)
    {
        if (!File.Exists(verbs.Config)) return;
        if (!File.Exists(verbs.Input)) return;

        var config = JsonSerializer.Deserialize<ADVConfig>(File.ReadAllText(verbs.Config), ADVConfigSourceGenerationContext.Default.ADVConfig);

        if (config == null) return;
        
        ADVConfig.Instance.Commands = config.Commands;

        var parsedScript = new CParsedScript();
        var sr = new StreamReader(verbs.Input);

        var type = sr.ReadLine();
        if (type == null) return;
        {
            var typeSplit = type.Split(' ');
            if (typeSplit[0] == "Type:") parsedScript.Type = typeSplit[1];
        }

        var version = sr.ReadLine();
        if (version == null) return;
        {
            var versionSplit = version.Split(' ');
            if (versionSplit[0] == "Version:") parsedScript.Version = Convert.ToUInt32(versionSplit[1]);
        }

        var flag = sr.ReadLine();
        if (flag == null) return;
        {
            var flagSplit = flag.Split(' ');
            if (flagSplit[0] == "Flag:") parsedScript.Flag = Convert.ToUInt32(flagSplit[1]);
        }
        
        var command = sr.ReadLine();
        if (command is not "Commands:") return;
        command = sr.ReadLine();
        
        while (command != null)
        {
            var parsedCommand = new SParsedCommand();

            var cmdName = CmdNameRegex();
            var cmdNameMatch = cmdName.Match(command);

            parsedCommand.Name = cmdNameMatch.Value.Trim();
            
            var cmdArgs = CmdArgsRegex();
            var cmdArgsMatch = cmdArgs.Match(command).Groups[1].Value.Split([','], StringSplitOptions.TrimEntries);

            var strFlag = 0;
            var cmdIdx = 0;
            
            foreach (var cmdArg in cmdArgsMatch)
            {
                var parsedArg = new SParsedArg();
                if (int.TryParse(cmdArg, out var arg))
                {
                    parsedArg.IsInt = true;
                    parsedArg.Value = arg;
                }
                else
                {
                    parsedArg.IsString = true;
                    parsedArg.StrValue = cmdArg;
                    strFlag += 1 << cmdIdx;
                }

                cmdIdx++;
                parsedCommand.Args.Add(parsedArg);
            }

            parsedCommand.StrFlag = strFlag;
            
            parsedScript.Commands.Add(parsedCommand);
            
            command = sr.ReadLine();
        }

        var script = parsedScript.Compile();
        
        File.WriteAllBytes(verbs.Output!, script.Write());
    }

    static void Decompile(DecompilerVerbs verbs)
    {
        if (!File.Exists(verbs.Config)) return;
        if (!File.Exists(verbs.Input)) return;

        var config = JsonSerializer.Deserialize<ADVConfig>(File.ReadAllText(verbs.Config), ADVConfigSourceGenerationContext.Default.ADVConfig);

        if (config == null) return;
        
        ADVConfig.Instance.Commands = config.Commands;

        var advScript = new CScriptData();
        advScript.Read(File.ReadAllBytes(verbs.Input));
        var parsedScript = new CParsedScript(advScript);
        
        var sw = new StreamWriter(verbs.Output!);
        sw.WriteLine("Type: " + parsedScript.Type);
        sw.WriteLine("Version: " + parsedScript.Version);
        sw.WriteLine("Flag: " + parsedScript.Flag);
        sw.WriteLine("Commands:");

        foreach (var command in parsedScript.Commands)
        {
            sw.Write("\t");
            sw.Write(command.Name + "(");
            for (var i = 0; i < command.Args.Count; i++)
            {
                var arg = command.Args[i];
                if (arg.IsInt) sw.Write(arg.Value);
                else sw.Write(arg.StrValue);
                
                if (i != command.Args.Count - 1) sw.Write(", ");
            }
            sw.WriteLine(")");
        }
        
        sw.Close();
    }

    [GeneratedRegex(@"^[^\(]+")]
    private static partial Regex CmdNameRegex();
    [GeneratedRegex(@"\(([^\)]+)\)")]
    private static partial Regex CmdArgsRegex();
}