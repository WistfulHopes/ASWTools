using BBScript.Compiler;
using BBScript.Config;
using sly.lexer;

namespace BBScript.Language;

public class BBSVarExpr : BBSExpression
{
    public LexerPosition? Position { get; set; }
    public BBSExpressionType Type => BBSExpressionType.VAR;
    public required string Name { get; init; }

    public string Dump()
    {
        return $"(VAR {Name})";
    }
    
    public void Compile(CompilerContext context)
    {
        context.Bytecode.AddRange(BitConverter.GetBytes(0).ToList());
        if (BBSConfig.Instance.Variables!.TryGetValue(Name, out var value))
            context.Bytecode.AddRange(BitConverter.GetBytes(value).ToList());
        else throw new KeyNotFoundException($"Variable {Name} not found!");
    }
}