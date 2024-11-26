using sly.lexer;

namespace BBScript.Parser;

public enum BBSLexer
{
    [Int] INT,
    [Hexa] HEX,
    [String("\"", "\\")] STRING,
    [Keyword("Const")] CONST,
    [Keyword("Var")] VAR,
    [AlphaNumDashId] IDENTIFIER,
    [Keyword("true")] TRUE = 7,
    [Keyword("false")] FALSE = 8,
    [Sugar("-")] MINUS,
    [Sugar("(")] LPAREN,
    [Sugar(")")] RPAREN,
    [Sugar(",")] COMMA,
    [Sugar(";")] SEMICOLON,
    [SingleLineComment("//")] SINGLECOMMENT,
    [MultiLineComment("/*", "*/")] MULTICOMMENT,
}