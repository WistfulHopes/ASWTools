using System.Text;
using BBScript.Compiler;
using sly.lexer;

namespace BBScript.Language;

public class BBSArgs : BBSAST
{
    public LexerPosition? Position { get; set; }
    public required List<BBSAST> Expressions;
    
    public string Dump()
    {
        var dump = new StringBuilder();
        Expressions.ForEach(e => dump.Append(e.Dump() + " "));
        return dump.ToString();
    }

    public void Compile(CompilerContext context)
    {
        foreach (var expr in Expressions)
        {
            expr.Compile(context);
        }
    }
}