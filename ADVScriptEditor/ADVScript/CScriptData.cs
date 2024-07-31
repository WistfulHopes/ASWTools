using System;
using System.Collections.Generic;
using System.Text;

namespace ADVScriptEditor.ADVScript;

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
    public int[] Arg { get; set; }
    public int StrFlag { get; set; }

    public SAdvScrCommand()
    {
        Arg = new int[14];
    }
}

public class SAdvScrStringHeader
{
    public uint StringIdx { get; set; }
    public uint Length { get; set; }
}

public class CScriptData
{
    public SAdvScrHeader m_ScriptHeader { get; set; }
    public SAdvScrParamHeader[] m_ParamHeader { get; set; }
    public SAdvScrCommand[] m_CommandList { get; set; }
    public SAdvScrStringHeader[] m_StringHeader { get; set; }
    public List<string> m_StringBuffer { get; set; }

    public CScriptData()
    {
        m_ScriptHeader = new SAdvScrHeader();
        m_ParamHeader = [new(), new(), new()];
        m_CommandList = Array.Empty<SAdvScrCommand>();
        m_StringHeader = Array.Empty<SAdvScrStringHeader>();
        m_StringBuffer = new List<string>();
    }
    
    public void Read(byte[] bytecode)
    {
        var pos = 0;
        
        m_ScriptHeader.Type = BitConverter.ToUInt32(bytecode);
        m_ScriptHeader.Version = BitConverter.ToUInt32(bytecode, 4);
        m_ScriptHeader.CommandNum = BitConverter.ToUInt32(bytecode, 8);
        m_ScriptHeader.Flag = BitConverter.ToUInt32(bytecode, 12);

        pos += 16;

        foreach (var paramHeader in m_ParamHeader)
        {
            paramHeader.Offset = BitConverter.ToUInt32(bytecode, pos);
            paramHeader.Num = BitConverter.ToUInt32(bytecode, pos + 4);
            pos += 16;
        }

        m_CommandList = new SAdvScrCommand[m_ScriptHeader.CommandNum];
        for (var i = 0; i < m_ScriptHeader.CommandNum; i++)
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

            m_CommandList[i] = command;
        }

        m_StringHeader = new SAdvScrStringHeader[m_ParamHeader[1].Num];
        for (var i = 0; i < m_ParamHeader[1].Num; i++)
        {
            var stringHeader = new SAdvScrStringHeader
            {
                StringIdx = BitConverter.ToUInt32(bytecode, pos),
                Length = BitConverter.ToUInt32(bytecode, pos + 4)
            };
            pos += 16;

            m_StringHeader[i] = stringHeader;
        }
        
        m_StringBuffer = [..new string[m_ParamHeader[1].Num]];
        for (var i = 0; i < m_ParamHeader[1].Num; i++)
        {
            if (m_StringHeader[i].Length == 0) m_StringBuffer[i] = "";
            else m_StringBuffer[i] = Encoding.ASCII.GetString(bytecode, pos, (int)m_StringHeader[i].Length);
            pos += (int)m_StringHeader[i].Length + 1;
        }
    }

    public byte[] Write()
    {
        var output = new List<byte>();
        
        output.AddRange(BitConverter.GetBytes(m_ScriptHeader.Type));
        output.AddRange(BitConverter.GetBytes(m_ScriptHeader.Version));
        output.AddRange(BitConverter.GetBytes(m_ScriptHeader.CommandNum));
        output.AddRange(BitConverter.GetBytes(m_ScriptHeader.Flag));
        
        foreach (var paramHeader in m_ParamHeader)
        {
            output.AddRange(BitConverter.GetBytes(paramHeader.Offset));
            output.AddRange(BitConverter.GetBytes(paramHeader.Num));
            output.AddRange(BitConverter.GetBytes((ulong)0));
        }

        foreach (var command in m_CommandList)
        {
            output.AddRange(BitConverter.GetBytes(command.Command));
            foreach (var arg in command.Arg)
            {
                output.AddRange(BitConverter.GetBytes(arg));
            }
            output.AddRange(BitConverter.GetBytes(command.StrFlag));
        }

        foreach (var stringHeader in m_StringHeader)
        {
            output.AddRange(BitConverter.GetBytes(stringHeader.StringIdx));
            output.AddRange(BitConverter.GetBytes(stringHeader.Length));
            output.AddRange(BitConverter.GetBytes((ulong)0));
        }

        foreach (var @string in m_StringBuffer)
        {
            if (@string != "") output.AddRange(Encoding.ASCII.GetBytes(@string));
            output.Add(0);
        }
        
        return output.ToArray();
    }
}