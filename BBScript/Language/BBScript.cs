using System.Text;
using BBScript.Compiler;
using sly.lexer;

namespace BBScript.Language;

public class BBScript : BBSAST
{
    public LexerPosition? Position { get; set; }
    public List<BBSAST>? Instructions;
    public string Dump()
    {
        var dump = new StringBuilder();
        dump.Append($"(SCRIPT [ ");
        Instructions!.ForEach(e => dump.AppendLine("\t" + e.Dump()));
        dump.Append("] )");
        return dump.ToString();
    }

    public void Compile(CompilerContext context)
    {
        foreach (var inst in Instructions!)
        {
            inst.Compile(context);
        }
    }
}