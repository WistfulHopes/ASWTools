using BBScript.Compiler;
using sly.lexer;

namespace BBScript.Language;

public class BBSBoolExpr : BBSExpression
{
    public LexerPosition? Position { get; set; }
    public BBSExpressionType Type => BBSExpressionType.BOOL;
    public bool Value { get; set; }

    public string Dump()
    {
        return $"(BOOL {Value})";
    }

    public void Compile(CompilerContext context)
    {
        context.Bytecode.AddRange(BitConverter.GetBytes(Value ? 1 : 0).ToList());
    }
}