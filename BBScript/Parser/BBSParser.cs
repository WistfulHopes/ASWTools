using sly.lexer;
using sly.parser.generator;
using BBScript.Language;
using sly.parser.parser;

namespace BBScript.Parser;

[ParserRoot("root")]
public class BBSParser
{
    [Production("root : instruction +")]
    public BBSAST root_instruction_(List<BBSAST> instructions)
    {
        return new Language.BBScript
        {
            Instructions = instructions
        };
    }

    [Production("instruction : IDENTIFIER LPAREN args? RPAREN SEMICOLON")]
    public BBSAST instruction_IDENTIFIER_LPAREN_args_RPAREN_SEMICOLON(Token<BBSLexer> instName,
        Token<BBSLexer> lParen, ValueOption<BBSAST> args, Token<BBSLexer> rParen, Token<BBSLexer> semicolon)
    {
        return new BBSInstruction
        {
            Name = instName.StringWithoutQuotes,
            Args = args.Match(a => a, () => new BBSArgs
            {
                Expressions = []
            }) as BBSArgs
        };
    }

    [Production("const : CONST LPAREN INT RPAREN")]
    public BBSAST const_CONST_LPAREN_INT_RPAREN(Token<BBSLexer> @const, Token<BBSLexer> @lParen,
        Token<BBSLexer> value, Token<BBSLexer> @rParen)
    {
        return new BBSConstExpr
        {
            Value = value.IntValue,
        };
    }

    [Production("const : CONST LPAREN MINUS INT RPAREN")]
    public BBSAST const_CONST_LPAREN_MINUS_INT_RPAREN(Token<BBSLexer> @const, Token<BBSLexer> @lParen,
        Token<BBSLexer> @minus, Token<BBSLexer> value, Token<BBSLexer> @rParen)
    {
        return new BBSConstExpr
        {
            Value = -value.IntValue,
        };
    }

    [Production("var : VAR LPAREN IDENTIFIER RPAREN")]
    public BBSAST var_VAR_LPAREN_IDENTIFIER_RPAREN(Token<BBSLexer> var, Token<BBSLexer> lParen,
        Token<BBSLexer> name, Token<BBSLexer> rParen)
    {
        return new BBSVarExpr
        {
            Name = name.StringWithoutQuotes,
        };
    }

    [Production("expr : INT")]
    public BBSAST expr_INT(Token<BBSLexer> value)
    {
        return new BBSIntExpr
        {
            Value = value.IntValue,
        };
    }

    [Production("expr : MINUS INT")]
    public BBSAST expr_MINUS_INT(Token<BBSLexer> @minus, Token<BBSLexer> value)
    {
        return new BBSIntExpr
        {
            Value = -value.IntValue,
        };
    }

    [Production("expr : HEX")]
    public BBSAST expr_HEX(Token<BBSLexer> value)
    {
        return new BBSHexExpr
        {
            Value = value.HexaIntValue,
        };
    }

    [Production("expr : STRING")]
    public BBSAST expr_STRING(Token<BBSLexer> value)
    {
        return new BBSStrExpr
        {
            Value = value.StringWithoutQuotes,
        };
    }

    [Production("expr : IDENTIFIER")]
    public BBSAST expr_IDENTIFIER(Token<BBSLexer> name)
    {
        return new BBSEnumExpr
        {
            Name = name.StringWithoutQuotes,
        };
    }

    [Production("expr : const")]
    public BBSAST expr_const(BBSAST @const)
    {
        return @const;
    }

    [Production("expr : var")]
    public BBSAST expr_var(BBSAST var)
    {
        return var;
    }

    [Production("args : expr (COMMA expr) *")]
    public BBSAST args_expr_COMMA_expr_(BBSAST expr, List<Group<BBSLexer, BBSAST>> groups)
    {
        var expressions = new List<BBSAST> { expr };
        expressions.AddRange(groups.Select(group => group.Value(1)));

        return new BBSArgs
        {
            Expressions = expressions,
        };
    }
}