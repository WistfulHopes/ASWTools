using System.Text;

namespace ADVScript.ADVScript;

public class SAdvScrHeader
{
    public uint Type { get; set; }
    public uint Version { get; set; }
    public uint CommandNum { get; set; }
    public uint Flag { get; set; }
}

public class SAdvScrParamHeader
{
    public uint Offset { get; set; }
    public uint Num { get; set; }
}

public class SAdvScrCommand
{
    public int Command { get; set; }
    public int[] Arg { get; } = new int[14];
    public int StrFlag { get; set; }
}

public class SAdvScrStringHeader
{
    public uint StringIdx { get; init; }
    public uint Length { get; init; }
}

public class CScriptData
{
    public SAdvScrHeader ScriptHeader { get; set; } = new();
    public SAdvScrParamHeader[] ParamHeader { get; set; } = [new(), new(), new()];
    public SAdvScrCommand[] CommandList { get; set; } = [];
    public SAdvScrStringHeader[] StringHeader { get; set; } = [];
    public List<string> StringBuffer { get; set; } = new();

    public void Read(byte[] bytecode)
    {
        var pos = 0;
        
        ScriptHeader.Type = BitConverter.ToUInt32(bytecode);
        ScriptHeader.Version = BitConverter.ToUInt32(bytecode, 4);
        ScriptHeader.CommandNum = BitConverter.ToUInt32(bytecode, 8);
        ScriptHeader.Flag = BitConverter.ToUInt32(bytecode, 12);

        pos += 16;

        foreach (var paramHeader in ParamHeader)
        {
            paramHeader.Offset = BitConverter.ToUInt32(bytecode, pos);
            paramHeader.Num = BitConverter.ToUInt32(bytecode, pos + 4);
            pos += 16;
        }

        CommandList = new SAdvScrCommand[ScriptHeader.CommandNum];
        for (var i = 0; i < ScriptHeader.CommandNum; i++)
        {
            var command = new SAdvScrCommand
            {
                Command = BitConverter.ToInt32(bytecode, pos)
            };
            pos += 4;
            
            for (var j = 0; j < 14; j++)
            {
                command.Arg[j] = BitConverter.ToInt32(bytecode, pos);
                pos += 4;
            }
            
            command.StrFlag = BitConverter.ToInt32(bytecode, pos);
            pos += 4;

            CommandList[i] = command;
        }

        StringHeader = new SAdvScrStringHeader[ParamHeader[1].Num];
        for (var i = 0; i < ParamHeader[1].Num; i++)
        {
            var stringHeader = new SAdvScrStringHeader
            {
                StringIdx = BitConverter.ToUInt32(bytecode, pos),
                Length = BitConverter.ToUInt32(bytecode, pos + 4)
            };
            pos += 16;

            StringHeader[i] = stringHeader;
        }
        
        StringBuffer = [..new string[ParamHeader[1].Num]];
        for (var i = 0; i < ParamHeader[1].Num; i++)
        {
            if (StringHeader[i].Length == 0) StringBuffer[i] = "";
            else StringBuffer[i] = Encoding.ASCII.GetString(bytecode, pos, (int)StringHeader[i].Length);
            pos += (int)StringHeader[i].Length + 1;
        }
    }

    public byte[] Write()
    {
        var output = new List<byte>();
        
        output.AddRange(BitConverter.GetBytes(ScriptHeader.Type));
        output.AddRange(BitConverter.GetBytes(ScriptHeader.Version));
        output.AddRange(BitConverter.GetBytes(ScriptHeader.CommandNum));
        output.AddRange(BitConverter.GetBytes(ScriptHeader.Flag));
        
        foreach (var paramHeader in ParamHeader)
        {
            output.AddRange(BitConverter.GetBytes(paramHeader.Offset));
            output.AddRange(BitConverter.GetBytes(paramHeader.Num));
            output.AddRange(BitConverter.GetBytes((ulong)0));
        }

        foreach (var command in CommandList)
        {
            output.AddRange(BitConverter.GetBytes(command.Command));
            foreach (var arg in command.Arg)
            {
                output.AddRange(BitConverter.GetBytes(arg));
            }
            output.AddRange(BitConverter.GetBytes(command.StrFlag));
        }

        foreach (var stringHeader in StringHeader)
        {
            output.AddRange(BitConverter.GetBytes(stringHeader.StringIdx));
            output.AddRange(BitConverter.GetBytes(stringHeader.Length));
            output.AddRange(BitConverter.GetBytes((ulong)0));
        }

        foreach (var @string in StringBuffer)
        {
            if (@string != "") output.AddRange(Encoding.ASCII.GetBytes(@string));
            output.Add(0);
        }
        
        return output.ToArray();
    }
}