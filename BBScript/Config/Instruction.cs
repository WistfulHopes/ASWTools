namespace BBScript.Config;

public enum IndentType
{
    None,
    Begin,
    End,
    ScopeBegin,
    ScopeEnd,
    Cell,
    CellEnd,
}

public enum ArgType
{
    BOOL,
    S8,
    S16,
    S32,
    U8,
    U16,
    U32,
    Enum,
    C16BYTE,
    C32BYTE,
    C64BYTE,
    C128BYTE,
    C256BYTE,
    COperand,
}

public struct Arg
{
    public ArgType Type { get; set; }
    public string Name { get; set; }
    public string? EnumName { get; set; }
}

public class Instruction
{
    public int Size { get; set; }
    public string? Name { get; set; }
    public IndentType Indent { get; set; }
    public List<Arg>? Args { get; set; }
}