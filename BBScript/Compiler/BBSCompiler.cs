﻿using System.Text;
using BBScript.Language;
using BBScript.Parser;
using sly.parser;
using sly.parser.generator;

namespace BBScript.Compiler;

public class BBSCompiler
{
    private readonly Parser<BBSLexer, BBSAST> _bbsParser;

    public BBSCompiler()
    {
        var parser = new BBSParser();
        var builder = new ParserBuilder<BBSLexer, BBSAST>();
        var parserBuildResult = builder.BuildParser(parser, ParserType.EBNF_LL_RECURSIVE_DESCENT, "root");
        _bbsParser = parserBuildResult.Result;
    }

    public byte[] Compile(string source)
    {
        var result = _bbsParser.Parse(source);
        if (!result.IsOk) throw new InvalidDataException("Could not parse script!\n" + result);
        var ast = result.Result;
            
        var context = new CompilerContext();
        ast.Compile(context);
            
        var output = new List<byte>();
        output.AddRange(BitConverter.GetBytes(context.JumpEntryTable.Count).ToList());
        foreach (var jumpEntry in context.JumpEntryTable)
        {
            output.AddRange(Encoding.ASCII.GetBytes(jumpEntry.Value));
            for (var i = 0; i < 32 - jumpEntry.Value.Length; i++) output.Add(0);
            output.AddRange(BitConverter.GetBytes(jumpEntry.Key).ToList());
        }
        
        output.AddRange(context.Bytecode);
        return output.ToArray();
    }
}