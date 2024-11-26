using BBScript.Compiler;
using BBScript.Config;
using sly.lexer;

namespace BBScript.Language;

public class BBSVarExpr : BBSExpression
{
    public LexerPosition? Position { get; set; }
    public BBSExpressionType Type => BBSExpressionType.VAR;
    public required string Name { get; init; }
    
    public required int Value { get; init; }

    public string Dump()
    {
        return $"(VAR {Name})";
    }
    
    public void Compile(CompilerContext context)
    {
        context.Bytecode.AddRange(BitConverter.GetBytes(2).ToList());
        if (Name.Length == 0)
        {
            context.Bytecode.AddRange(BitConverter.GetBytes(Value).ToList());
        }
        else if (BBSConfig.Instance.Variables!.TryGetValue(Name, out var value))
            context.Bytecode.AddRange(BitConverter.GetBytes(value).ToList());
        else throw new KeyNotFoundException($"Variable {Name} not found!");
    }
}