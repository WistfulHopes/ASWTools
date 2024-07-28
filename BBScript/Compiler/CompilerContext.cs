namespace BBScript.Compiler;

public class CompilerContext
{
    public List<byte> Bytecode;
    public List<Tuple<int, string>> JumpEntryTable;

    public CompilerContext()
    {
        Bytecode = new List<byte>();
        JumpEntryTable = new List<Tuple<int, string>>();
    }
}