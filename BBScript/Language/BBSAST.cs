using System.Formats.Asn1;
using BBScript.Compiler;
using sly.lexer;

namespace BBScript.Language;

public interface BBSAST
{
    LexerPosition? Position { get; set; }

    string Dump();
    void Compile(CompilerContext context);
}