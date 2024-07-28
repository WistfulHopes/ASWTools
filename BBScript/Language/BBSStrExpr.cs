using System.Text;
using BBScript.Compiler;
using sly.lexer;

namespace BBScript.Language;

public class BBSStrExpr : BBSExpression
{
    public LexerPosition? Position { get; set; }
    public BBSExpressionType Type => BBSExpressionType.STRING;
    public required string Value { get; init; }
    public int Length { get; set; }
    
    public string Dump()
    {
        return $"(STR \"{Value}\")";
    }
    
    public void Compile(CompilerContext context)
    {
        if (Value.Length > Length) throw new InvalidDataException();
        context.Bytecode!.AddRange(Encoding.ASCII.GetBytes(Value));
        for (var i = 0; i < Length - Value.Length; i++) context.Bytecode.Add(0);
    }
}