using BBScript.Compiler;
using BBScript.Config;
using sly.lexer;

namespace BBScript.Language;

public class BBSEnumExpr : BBSExpression
{
    public LexerPosition? Position { get; set; }
    public BBSExpressionType Type => BBSExpressionType.ENUM;
    public required string Name { get; init; }

    public string Dump()
    {
        return $"(ENUM {Name})";
    }

    public void Compile(CompilerContext context)
    {
        foreach (var @enum in BBSConfig.Instance.Enums?.Values!)
        {
            if (!@enum!.TryGetValue(Name, out var value)) continue;
            
            context.Bytecode.AddRange(BitConverter.GetBytes(value).ToList());
            return;
        }

        throw new KeyNotFoundException();
    }
}