using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;

namespace ADVScript.ADVScript;

public record struct SParsedArg()
{
    public bool IsInt { get; set; } 
    public bool IsString { get; set; } 
    public int Value { get; set; }
    public string StrValue { get; set; } = "";
}

public struct SParsedCommand() : IEquatable<SParsedCommand>
{
    public string Name { get; set; } = "";
    public List<SParsedArg> Args { get; set; } = [];
    public int StrFlag { get; set; }

    public bool Equals(SParsedCommand other)
    {
        return Name == other.Name && Args.Equals(other.Args) && StrFlag == other.StrFlag;
    }

    public override bool Equals(object? obj)
    {
        return obj is SParsedCommand other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Args, StrFlag);
    }

    public static bool operator ==(SParsedCommand left, SParsedCommand right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SParsedCommand left, SParsedCommand right)
    {
        return !(left == right);
    }
}

public class CParsedScript
{
    public string Type { get; set; }
    public uint Version { get; set; }
    public uint Flag { get; set; }
    public ObservableCollection<SParsedCommand> Commands { get; set; }
    
    public CParsedScript()
    {
        Type = "";
        Commands = [];
    }

    public CParsedScript(CScriptData inAdvScript)
    {
        Type = Encoding.ASCII.GetString(BitConverter.GetBytes(inAdvScript.ScriptHeader.Type), 0 ,3);
        Version = inAdvScript.ScriptHeader.Version;
        Flag = inAdvScript.ScriptHeader.Flag;
        Commands = new ObservableCollection<SParsedCommand>();

        foreach (var rawCommand in inAdvScript.CommandList)
        {
            var command = new SParsedCommand
            {
                Name = inAdvScript.StringBuffer[rawCommand.Command],
                StrFlag = rawCommand.StrFlag
            };

            if (!ADVConfig.Instance.Commands!.TryGetValue(command.Name.ToLower(), out var argTypes))
            {
                Console.WriteLine("Failed to find command " + command.Name.ToLower() + " in the configuration! Make an issue on the GitHub repository.");
                continue;
            }

            for (var i = 0; i < argTypes.Count; i++)
            {
                var arg = new SParsedArg();
                switch (argTypes[i])
                {
                    case ADVArg.Int:
                        arg.IsInt = true;
                        arg.IsString = false;
                        arg.Value = rawCommand.Arg[i];
                        arg.StrValue = "";
                        break;
                    case ADVArg.String:
                        arg.IsInt = false;
                        arg.IsString = true;
                        arg.Value = 0;
                        arg.StrValue = inAdvScript.StringBuffer[rawCommand.Arg[i]];
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                command.Args.Add(arg);
            }
            
            Commands.Add(command);
        }
    }

    public CScriptData Compile()
    {
        var typeBytes = Type.ToCharArray().Select(c => (byte)c).ToList();
        CollectionsMarshal.SetCount(typeBytes, 4);
        var script = new CScriptData
        {
            ScriptHeader =
            {
                Type = BitConverter.ToUInt32(typeBytes.ToArray()),
                Version = Version,
                CommandNum = (uint)Commands.Count,
                Flag = Flag
            }
        };

        script.StringBuffer.Add("dummy");
        
        script.ParamHeader[0].Offset = 0x40;
        script.ParamHeader[0].Num = (uint)Commands.Count;

        script.CommandList = new SAdvScrCommand[Commands.Count];

        for (var i = 0; i < Commands.Count; i++)
        {
            var command = Commands[i];
            var scrCommand = new SAdvScrCommand();
            for (var j = 0; j < 14; j++)
            {
                if (j >= command.Args.Count) break;

                var arg = command.Args[j];
                if (arg.IsString)
                {
                    var index = script.StringBuffer.IndexOf(arg.StrValue);
                    if (index == -1)
                    {
                        script.StringBuffer.Add(arg.StrValue);
                        scrCommand.Arg[j] = script.StringBuffer.Count - 1;
                    }
                    else
                    {
                        scrCommand.Arg[j] = index;
                    }
                }
                else
                {
                    scrCommand.Arg[j] = arg.Value;
                }
            }

            scrCommand.StrFlag = command.StrFlag;
            script.CommandList[i] = scrCommand;
        }

        for (var i = 0; i < Commands.Count; i++)
        {
            var scrCommand = script.CommandList[i];
            var index = script.StringBuffer.IndexOf(Commands[i].Name);
            if (index == -1)
            {
                script.StringBuffer.Add(Commands[i].Name);
                scrCommand.Command = script.StringBuffer.Count - 1;
            }
            else
            {
                scrCommand.Command = index;
            }
        }

        script.ParamHeader[1].Offset = script.ParamHeader[0].Offset + script.ParamHeader[0].Num * 0x40;
        script.ParamHeader[1].Num = (uint)script.StringBuffer.Count;

        script.ParamHeader[2].Offset = script.ParamHeader[1].Offset + script.ParamHeader[1].Num * 0x10;

        script.StringHeader = new SAdvScrStringHeader[script.StringBuffer.Count];
        var stringIdx = 0;
        
        for (var i = 0; i < script.StringBuffer.Count; i++)
        {
            script.StringHeader[i] = new SAdvScrStringHeader
            {
                StringIdx = (uint)stringIdx,
                Length = (uint)script.StringBuffer[i].Length
            };
            stringIdx += script.StringBuffer[i].Length + 1;
        }

        script.ParamHeader[2].Num = (uint)stringIdx;
        
        return script;
    }
}