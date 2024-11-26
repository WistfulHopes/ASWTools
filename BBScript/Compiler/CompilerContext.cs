namespace BBScript.Compiler;

public class CompilerContext
{
    public List<byte> Bytecode;
    public Dictionary<int, string> JumpEntryTable;

    public CompilerContext()
    {
        Bytecode = new List<byte>();
        JumpEntryTable = new Dictionary<int, string>();
    }
}