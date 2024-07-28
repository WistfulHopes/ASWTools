using BBScript.Compiler;
using sly.lexer;

namespace BBScript.Language;

public class BBSConstExpr : BBSExpression
{
    public LexerPosition? Position { get; set; }
    public BBSExpressionType Type => BBSExpressionType.CONST;
    public int Value { get; init; }
    
    public string Dump()
    {
        return $"(CONST {Value})";
    }

    public void Compile(CompilerContext context)
    {
        context.Bytecode.AddRange(BitConverter.GetBytes(0).ToList());
        context.Bytecode.AddRange(BitConverter.GetBytes(Value).ToList());
    }
}