using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DynamicData;

namespace ADVScriptEditor.ADVScript;

public struct SParsedArg
{
    public bool IsInt { get; set; } 
    public bool IsString { get; set; } 
    public int Value { get; set; }
    public string StrValue { get; set; }
    
    public SParsedArg()
    {
        StrValue = "";
    }
}

public struct SParsedCommand
{
    public string Name { get; set; }
    public List<SParsedArg> Args { get; set; }
    public int StrFlag { get; set; }

    public SParsedCommand()
    {
        Args = new List<SParsedArg>();
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
        Commands = new ObservableCollection<SParsedCommand>();
    }

    public CParsedScript(CScriptData inAdvScript)
    {
        Type = Encoding.ASCII.GetString(BitConverter.GetBytes(inAdvScript.m_ScriptHeader.Type), 0 ,3);
        Version = inAdvScript.m_ScriptHeader.Version;
        Flag = inAdvScript.m_ScriptHeader.Flag;
        Commands = new ObservableCollection<SParsedCommand>();

        foreach (var rawCommand in inAdvScript.m_CommandList)
        {
            var command = new SParsedCommand
            {
                Name = inAdvScript.m_StringBuffer[rawCommand.Command],
                StrFlag = rawCommand.StrFlag
            };

            if (!AdvConfig.Instance.Commands!.TryGetValue(command.Name, out var argTypes)) continue;

            for (var i = 0; i < argTypes.Count; i++)
            {
                var arg = new SParsedArg();
                switch (argTypes[i])
                {
                    case AdvArg.Int:
                        arg.IsInt = true;
                        arg.IsString = false;
                        arg.Value = rawCommand.Arg[i];
                        arg.StrValue = "Unused";
                        break;
                    case AdvArg.String:
                        arg.IsInt = false;
                        arg.IsString = true;
                        arg.Value = -1;
                        arg.StrValue = inAdvScript.m_StringBuffer[rawCommand.Arg[i]];
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
            m_ScriptHeader =
            {
                Type = BitConverter.ToUInt32(typeBytes.ToArray()),
                Version = Version,
                CommandNum = (uint)Commands.Count,
                Flag = Flag
            }
        };

        script.m_StringBuffer.Add("dummy");
        
        script.m_ParamHeader[0].Offset = 0x40;
        script.m_ParamHeader[0].Num = (uint)Commands.Count;

        script.m_CommandList = new SAdvScrCommand[Commands.Count];

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
                    var index = script.m_StringBuffer.IndexOf(arg.StrValue);
                    if (index == -1)
                    {
                        script.m_StringBuffer.Add(arg.StrValue);
                        scrCommand.Arg[j] = script.m_StringBuffer.Count - 1;
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
            script.m_CommandList[i] = scrCommand;
        }

        for (var i = 0; i < Commands.Count; i++)
        {
            var scrCommand = script.m_CommandList[i];
            var index = script.m_StringBuffer.IndexOf(Commands[i].Name);
            if (index == -1)
            {
                script.m_StringBuffer.Add(Commands[i].Name);
                scrCommand.Command = script.m_StringBuffer.Count - 1;
            }
            else
            {
                scrCommand.Command = index;
            }
        }

        script.m_ParamHeader[1].Offset = script.m_ParamHeader[0].Offset + script.m_ParamHeader[0].Num * 0x40;
        script.m_ParamHeader[1].Num = (uint)script.m_StringBuffer.Count;

        script.m_ParamHeader[2].Offset = script.m_ParamHeader[1].Offset + script.m_ParamHeader[1].Num * 0x10;

        script.m_StringHeader = new SAdvScrStringHeader[script.m_StringBuffer.Count];
        var stringIdx = 0;
        
        for (var i = 0; i < script.m_StringBuffer.Count; i++)
        {
            script.m_StringHeader[i] = new SAdvScrStringHeader
            {
                StringIdx = (uint)stringIdx,
                Length = (uint)script.m_StringBuffer[i].Length
            };
            stringIdx += script.m_StringBuffer[i].Length + 1;
        }

        script.m_ParamHeader[2].Num = (uint)stringIdx;
        
        return script;
    }
}