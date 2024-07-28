using BBScript.Compiler;
using sly.lexer;

namespace BBScript.Language;

public class BBSIntExpr : BBSExpression
{
    public LexerPosition? Position { get; set; }
    public BBSExpressionType Type => BBSExpressionType.INT;
    public int Value { get; set; }
    
    public string Dump()
    {
        return $"(INT {Value})";
    }
    
    public void Compile(CompilerContext context)
    {
        context.Bytecode.AddRange(BitConverter.GetBytes(Value).ToList());
    }
}