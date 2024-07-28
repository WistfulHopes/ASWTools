using BBScript.Compiler;
using sly.lexer;

namespace BBScript.Language;

public class BBSHexExpr : BBSExpression
{
    public LexerPosition? Position { get; set; }
    public BBSExpressionType Type => BBSExpressionType.HEX;
    public long Value { get; init; }
    
    public string Dump()
    {
        return $"(HEX {0:Value})";
    }

    public void Compile(CompilerContext context)
    {
        context.Bytecode.AddRange(BitConverter.GetBytes(Value).ToList());
    }
}