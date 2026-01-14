using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommandLine;

namespace ATFText;

struct ATF_SFileHeader
{
    public uint Type;
    public uint TextNum;
}

struct ATF_SParamHeader
{
    public uint Offset;
    public uint Num;
    public uint Size;
}

struct ATF_STextHeader
{
    public uint NameIdx;
    public uint TextIdx;
    public List<uint> TextParam;
}

struct ATF_SStringHeader
{
    public uint Top;
    public uint Len;
}

struct CAdvTextData
{
    public ATF_SFileHeader m_FileHeader;
    public List<ATF_SParamHeader> m_ParamHeader;
    public List<ATF_STextHeader> m_TextHeader;
    public List<ATF_SStringHeader> m_StringHeader;
    public List<string> m_String;
    public List<string> m_WString;
}

internal partial class Program
{
    [Verb("build", HelpText = "Builds a source ATF file to binary.")]
    private class BuildVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input file.")]
        public string? Input { get; set; }

        [Option('o', "output", HelpText = "Output file.")]
        public string? Output { get; set; }
    }

    [Verb("parse", HelpText = "Parses a binary ATF file to text.")]
    private class ParseVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input file.")]
        public string? Input { get; set; }

        [Option('o', "output", HelpText = "Output file.")]
        public string? Output { get; set; }
    }

    static void Main(string[] args)
    {
        var p = Parser.Default.ParseArguments<BuildVerbs, ParseVerbs>(args);

        p.WithParsed<BuildVerbs>(Build).WithParsed<ParseVerbs>(Decompile);
    }

    static void Build(BuildVerbs verbs)
    {
        if (!File.Exists(verbs.Input)) return;

        var textData = new CAdvTextData();
        var sr = new StreamReader(verbs.Input);

        List<string> keys = [];
        List<string> values = [];

        while (sr.ReadLine() is { } line)
        {
            if (line.StartsWith("Key: "))
            {
                keys.Add(line[5..]);
            }
            else if (line.StartsWith("Value: "))
            {
                values.Add(line[7..]);
            }
            else
            {
                values[^1] += line;
            }
        }

        textData.m_String = [];
        var strLen = 0;
        foreach (var key in keys)
        {
            textData.m_String.Add(key);
            strLen += key.Length + 2;
        }

        textData.m_WString = [];
        var wstrLen = 0;
        foreach (var value in values)
        {
            textData.m_WString.Add(value);
            wstrLen += value.Length + 1;
        }
        
        sr.Close();

        textData.m_FileHeader.Type = 0x465441;
        textData.m_FileHeader.TextNum = (uint)keys.Count;

        textData.m_ParamHeader = [];
        var textHeaderParam = new ATF_SParamHeader
        {
            Offset = 0x50,
            Num = (uint)keys.Count,
            Size = 0x30 * (uint)keys.Count,
        };
        textData.m_ParamHeader.Add(textHeaderParam);

        var stringHeaderParam = new ATF_SParamHeader
        {
            Offset = textHeaderParam.Offset + textHeaderParam.Size,
            Num = (uint)(keys.Count + values.Count),
            Size = 0x10 * (uint)(keys.Count + values.Count),
        };
        textData.m_ParamHeader.Add(stringHeaderParam);

        var stringParam = new ATF_SParamHeader
        {
            Offset = stringHeaderParam.Offset + stringHeaderParam.Size,
            Num = (uint)strLen,
            Size = (uint)strLen,
        };
        textData.m_ParamHeader.Add(stringParam);

        var wstringParam = new ATF_SParamHeader
        {
            Offset = stringParam.Offset + stringParam.Size,
            Num = (uint)wstrLen,
            Size = (uint)(wstrLen * 2),
        };
        textData.m_ParamHeader.Add(wstringParam);

        textData.m_TextHeader = [];
        for (var i = 0; i < keys.Count; i++)
        {
            var textHeader = new ATF_STextHeader
            {
                NameIdx = (uint)i * 2,
                TextIdx = (uint)i * 2 + 1,
                TextParam = [0, 0, 0, 0, 0, 0]
            };
            textData.m_TextHeader.Add(textHeader);
        }

        uint strTop = 0;
        uint wstrTop = 0;
        textData.m_StringHeader = [];
        for (var i = 0; i < keys.Count + values.Count; i++)
        {
            ATF_SStringHeader stringHeader;
            if (i % 2 == 0)
            {
                stringHeader = new ATF_SStringHeader
                {
                    Top = strTop,
                    Len = (uint)keys[i / 2].Length + 1
                };
                strTop += stringHeader.Len + 1;
            }
            else
            {
                stringHeader = new ATF_SStringHeader
                {
                    Top = wstrTop,
                    Len = (uint)values[(i - 1) / 2].Length
                };
                wstrTop += stringHeader.Len + 1;
            }
            textData.m_StringHeader.Add(stringHeader);
        }

        using var fs = new FileStream(verbs.Output!, FileMode.Create);
        using var bw = new BinaryWriter(fs);
        
        bw.Write(textData.m_FileHeader.Type);
        bw.Write(textData.m_FileHeader.TextNum);
        bw.Write(0);
        bw.Write(0);
        foreach (var paramHeader in textData.m_ParamHeader)
        {
            bw.Write(paramHeader.Offset);
            bw.Write(paramHeader.Num);
            bw.Write(paramHeader.Size);
            bw.Write(0);
        }
        foreach (var textHeader in textData.m_TextHeader)
        {
            bw.Write(textHeader.NameIdx);
            bw.Write(textHeader.TextIdx);
            foreach (var param in textHeader.TextParam)
            {
                bw.Write(param);
            }
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
        }
        foreach (var stringHeader in textData.m_StringHeader)
        {
            bw.Write(stringHeader.Top);
            bw.Write(stringHeader.Len);
            bw.Write(0);
            bw.Write(0);
        }
        foreach (var @string in textData.m_String)
        {
            if (@string != "") bw.Write(Encoding.ASCII.GetBytes(@string));
            bw.Write((ushort)0);
        }
        foreach (var wstring in textData.m_WString)
        {
            if (wstring != "") bw.Write(Encoding.Unicode.GetBytes(wstring));
            bw.Write((ushort)0);
        }
        
        bw.Close();
    }

    static void Decompile(ParseVerbs verbs)
    {
        if (!File.Exists(verbs.Input)) return;

        var textData = new CAdvTextData();
        using var fs = new FileStream(verbs.Input, FileMode.Open);
        using var br = new BinaryReader(fs);

        textData.m_FileHeader.Type = br.ReadUInt32();
        textData.m_FileHeader.TextNum = br.ReadUInt32();
        br.BaseStream.Seek(8, SeekOrigin.Current);

        textData.m_ParamHeader = [];

        for (var i = 0; i < 4; i++)
        {
            var paramHeader = new ATF_SParamHeader
            {
                Offset = br.ReadUInt32(),
                Num = br.ReadUInt32(),
                Size = br.ReadUInt32()
            };
            textData.m_ParamHeader.Add(paramHeader);
            br.BaseStream.Seek(4, SeekOrigin.Current);
        }

        br.BaseStream.Seek(textData.m_ParamHeader[0].Offset, SeekOrigin.Begin);
        textData.m_TextHeader = [];
        for (var i = 0; i < textData.m_ParamHeader[0].Num; i++)
        {
            var textHeader = new ATF_STextHeader
            {
                NameIdx = br.ReadUInt32(),
                TextIdx = br.ReadUInt32(),
                TextParam = []
            };
            for (var j = 0; j < 6; j++)
            {
                textHeader.TextParam.Add(br.ReadUInt32());
            }

            textData.m_TextHeader.Add(textHeader);
            br.BaseStream.Seek(16, SeekOrigin.Current);
        }

        br.BaseStream.Seek(textData.m_ParamHeader[1].Offset, SeekOrigin.Begin);
        textData.m_StringHeader = [];
        for (var i = 0; i < textData.m_ParamHeader[1].Num; i++)
        {
            var stringHeader = new ATF_SStringHeader
            {
                Top = br.ReadUInt32(),
                Len = br.ReadUInt32()
            };
            textData.m_StringHeader.Add(stringHeader);
            br.BaseStream.Seek(8, SeekOrigin.Current);
        }

        br.BaseStream.Seek(textData.m_ParamHeader[2].Offset, SeekOrigin.Begin);
        textData.m_String = [];
        for (var i = 0; i < textData.m_TextHeader.Count; i++)
        {
            br.BaseStream.Seek(textData.m_ParamHeader[2].Offset + textData.m_StringHeader[(int)textData.m_TextHeader[i].NameIdx].Top, SeekOrigin.Begin);
            var str = textData.m_StringHeader[(int)textData.m_TextHeader[i].NameIdx].Len == 0
                ? ""
                : Encoding.ASCII.GetString(br.ReadBytes((int)textData.m_StringHeader[(int)textData.m_TextHeader[i].NameIdx].Len - 1), 0,
                    (int)textData.m_StringHeader[(int)textData.m_TextHeader[i].NameIdx].Len - 1);
            textData.m_String.Add(str);
        }
        
        br.BaseStream.Seek(textData.m_ParamHeader[3].Offset, SeekOrigin.Begin);
        textData.m_WString = [];
        for (var i = 0; i < textData.m_TextHeader.Count; i++)
        {
            br.BaseStream.Seek(textData.m_ParamHeader[3].Offset + textData.m_StringHeader[(int)textData.m_TextHeader[i].TextIdx].Top * 2, SeekOrigin.Begin);
            var str = textData.m_StringHeader[(int)textData.m_TextHeader[i].TextIdx].Len == 0
                ? ""
                : Encoding.Unicode.GetString(br.ReadBytes((int)textData.m_StringHeader[(int)textData.m_TextHeader[i].TextIdx].Len * 2), 0,
                    (int)textData.m_StringHeader[(int)textData.m_TextHeader[i].TextIdx].Len * 2);
            textData.m_WString.Add(str);
        }
        
        br.Close();

        var sw = new StreamWriter(verbs.Output!);

        for (var i = 0; i < textData.m_TextHeader.Count; i++)
        {
            sw.WriteLine("Key: " + textData.m_String[i]);
            sw.WriteLine("Value: " + textData.m_WString[i]);
        }

        sw.Close();
    }

    [GeneratedRegex(@"^[^\(]+")]
    private static partial Regex CmdNameRegex();

    [GeneratedRegex(@"\(([^\)]+)\)")]
    private static partial Regex CmdArgsRegex();
}