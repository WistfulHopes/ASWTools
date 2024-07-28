﻿using System.Text;
using BBScript.Compiler;
using BBScript.Config;
using sly.lexer;

namespace BBScript.Language;

public class BBSInstruction : BBSAST
{
    public LexerPosition? Position { get; set; }
    public required string Name;
    public required BBSArgs? Args;
    
    public string Dump()
    {
        var dump = new StringBuilder();
        dump.Append($"(INST {Name} [ ");
        if (Args != null) dump.Append(Args.Dump());
        dump.Append("] )");
        return dump.ToString();
    }

    public void Compile(CompilerContext context)
    {
        if (BBSConfig.Instance.JumpTableEntries!.Contains(Name))
        {
            context.JumpEntryTable.Add(new Tuple<int, string>(context.Bytecode.Count, (Args!.Expressions[0] as BBSStrExpr)!.Value));
        }
        
        var instruction = BBSConfig.Instance.Instructions!.Values.FirstOrDefault(inst => inst.Name == Name);
        if (instruction == null) throw new KeyNotFoundException();

        var id = BBSConfig.Instance.Instructions!.FirstOrDefault(x => x.Value == instruction).Key;
        context.Bytecode.AddRange(BitConverter.GetBytes(id).ToList());

        if (Args != null)
        {
            for (var i = 0; i < Args.Expressions.Count; i++)
            {
                if (i >= instruction.Args!.Count) throw new InvalidDataException();
                switch (instruction.Args[i].Type)
                {
                    case ArgType.BOOL:
                    case ArgType.S8:
                    case ArgType.S16:
                    case ArgType.S32:
                        if ((Args.Expressions[i] as BBSExpression)!.Type != BBSExpressionType.INT)
                            throw new InvalidDataException();
                        break;
                    case ArgType.U8:
                    case ArgType.U16:
                    case ArgType.U32:
                        if ((Args.Expressions[i] as BBSExpression)!.Type != BBSExpressionType.HEX)
                            throw new InvalidDataException();
                        break;
                    case ArgType.Enum:
                        if ((Args.Expressions[i] as BBSExpression)!.Type != BBSExpressionType.ENUM
                            && (Args.Expressions[i] as BBSExpression)!.Type != BBSExpressionType.INT)
                            throw new InvalidDataException();
                        break;
                    case ArgType.C16BYTE:
                        if ((Args.Expressions[i] as BBSExpression)!.Type != BBSExpressionType.STRING)
                            throw new InvalidDataException();
                        (Args.Expressions[i] as BBSStrExpr)!.Length = 16;
                        break;
                    case ArgType.C32BYTE:
                        if ((Args.Expressions[i] as BBSExpression)!.Type != BBSExpressionType.STRING)
                            throw new InvalidDataException();
                        (Args.Expressions[i] as BBSStrExpr)!.Length = 32;
                        break;
                    case ArgType.COperand:
                        if ((Args.Expressions[i] as BBSExpression)!.Type != BBSExpressionType.CONST
                            && (Args.Expressions[i] as BBSExpression)!.Type != BBSExpressionType.VAR)
                            throw new InvalidDataException();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        Args?.Compile(context);
    }
}