using System.Text;
using Pidgin;
using Pidgin.Comment;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace BBScript.Compiler;

public static class BBSCompiler
{
    static Parser<char, T> Tok<T>(Parser<char, T> p)
        => Try(p).Before(SkipWhitespaces);

    static Parser<char, char> Tok(char value) => Tok(Char(value));
    static Parser<char, string> Tok(string value) => Tok(String(value));

    private static Parser<char, T> Args<T>(Parser<char, T> parser)
        => parser.Between(Tok("("), Tok(")"));

    private static readonly Parser<char, char> _semicolon = Tok(';');
    private static readonly Parser<char, char> _quote = Tok('"');

    private static readonly Parser<char, BBS> _string =
        Token(c => c != '"')
            .ManyString()
            .Between(_quote)
            .Select<BBS>(value => new BBSStrExpr(value))
            .Labelled("string");

    private static readonly Parser<char, BBS> _int
        = Tok(Num)
            .Select<BBS>(value => new BBSIntExpr(value))
            .Labelled("integer");

    private static readonly Parser<char, BBS> _hex
        = Tok("0x")
            .Then(Tok(HexNum))
            .Select<BBS>(value => new BBSHexExpr(value))
            .Labelled("hexadecimal");

    private static readonly Parser<char, BBS> _bool
        = Tok("true")
            .Or(Tok("false"))
            .Select<BBS>(value => new BBSBoolExpr(bool.Parse(value)))
            .Labelled("boolean");

    private static readonly Parser<char, BBS> _enum =
        Tok(LetterOrDigit.Or(Tok('_')).ManyString())
            .Select<BBS>(name => new BBSEnumExpr(name))
            .Labelled("enumeration");

    private static readonly Parser<char, BBS> _arg =
        _string.Or(_hex).Or(_int).Or(_bool).Or(_enum)
            .Labelled("argument");

    private static readonly Parser<char, BBSInst> _inst =
        Tok(LetterOrDigit.Or(Tok('_')).ManyString())
            .Then(Args(_arg.Separated(Tok(",")).Select(args => args.ToArray())),
                (name, args) => new BBSInst(name, args))
            .Before(_semicolon)
            .Labelled("instruction");

    private static readonly Parser<char, BBSInst[]> _bbs =
        from _ in SkipWhitespaces
        from insts in _inst.Many()
        select insts.ToArray();

    public static byte[] Compile(string source, bool bigEndian = false)
    {
        var bbs = ParseOrThrow(source);

        var context = new CompilerContext();
        foreach (var inst in bbs)
        {
            inst.Compile(context, bigEndian);
        }

        var output = new List<byte>();
        var entryCount = BitConverter.GetBytes(context.JumpEntryTable.Count).ToList();
        if (bigEndian) entryCount.Reverse();
        output.AddRange(entryCount);
        foreach (var jumpEntry in context.JumpEntryTable)
        {
            output.AddRange(Encoding.ASCII.GetBytes(jumpEntry.Value));
            for (var i = 0; i < 32 - jumpEntry.Value.Length; i++) output.Add(0);
            var jumpEntryPos = BitConverter.GetBytes(jumpEntry.Key).ToList();
            if (bigEndian) jumpEntryPos.Reverse();
            output.AddRange(jumpEntryPos);
        }

        output.AddRange(context.Bytecode);
        return output.ToArray();
    }

    private static BBSInst[] ParseOrThrow(string source) => _bbs.ParseOrThrow(source);
}