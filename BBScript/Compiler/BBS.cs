using System.Text;
using BBScript.Config;

namespace BBScript.Compiler;

public abstract class BBS
{
    public abstract void Compile(CompilerContext context, bool bigEndian = false);
}

public class BBSInst : BBS
{
    public string Name;
    public BBS[] Args;

    public BBSInst(string name, params BBS[] args)
    {
        Name = name;
        Args = args;
    }
    
    public override string ToString()
    {
        var dump = new StringBuilder();
        dump.Append($"(INST {Name} [ ");
        foreach (var arg in Args) dump.Append(arg);
        dump.Append("] )");
        return dump.ToString();
    }

    public override void Compile(CompilerContext context, bool bigEndian = false)
    {
        if (BBSConfig.Instance.JumpTableEntries!.Contains(Name))
        {
            context.JumpEntryTable[context.Bytecode.Count] = (Args[0] as BBSStrExpr)!.Value;
        }

        Instruction? instruction = null;
        try
        {
            instruction = BBSConfig.Instance.Instructions!.Values.SingleOrDefault(inst => inst.Name == Name);
        }
        catch (InvalidOperationException exception)
        {
            Console.WriteLine("Duplicate instruction {0}!", Name);
            throw;
        }
        if (instruction == null)
        {
            if (Name.StartsWith("Unknown"))
            {
                instruction = BBSConfig.Instance.Instructions![int.Parse(Name[7..])];
            }
            else
            {
                throw new KeyNotFoundException($"Instruction {Name} not found!");
            }
        }

        var id = BBSConfig.Instance.Instructions!.Single(x => x.Value == instruction).Key;
        context.Bytecode.AddRange(BitConverter.GetBytes(id).ToList());

        if (Args.Length == 1 && (Args[0] as BBSEnumExpr)?.Name == "") Args = []; 
        
        if (Args.Length != instruction.Args!.Count) throw new InvalidDataException();

        for (int i = 0; i < Args.Length; i++)
        {
            switch (instruction.Args[i].Type)
            {
                case ArgType.BOOL:
                    if (Args[i] is not BBSBoolExpr)
                        throw new InvalidDataException(
                            $"Argument {i} should be a boolean, but it was {Args[i].GetType()}");
                    break;
                case ArgType.S8:
                case ArgType.S16:
                case ArgType.S32:
                    if (Args[i] is not BBSIntExpr)
                        throw new InvalidDataException(
                            $"Argument {i} should be an integer, but it was {Args[i].GetType()}");
                    break;
                case ArgType.U8:
                case ArgType.U16:
                case ArgType.U32:
                    if (Args[i] is not BBSHexExpr)
                        throw new InvalidDataException(
                            $"Argument {i} should be a hexadecimal number, but it was {Args[i].GetType()}");
                    break;
                case ArgType.Enum:
                    if (Args[i] is not BBSEnumExpr && Args[i] is not BBSIntExpr)
                        throw new InvalidDataException(
                            $"Argument {i} should be an enum value or an integer, but it was {Args[i].GetType()}");
                    break;
                case ArgType.C16BYTE:
                    if (Args[i] is not BBSStrExpr)
                        throw new InvalidDataException(
                            $"Argument {i} should be a string, but it was {Args[i].GetType()}");
                    (Args[i] as BBSStrExpr)!.Length = 16;
                    break;
                case ArgType.C32BYTE:
                    if (Args[i] is not BBSStrExpr)
                        throw new InvalidDataException(
                            $"Argument {i} should be a string, but it was {Args[i].GetType()}");
                    (Args[i] as BBSStrExpr)!.Length = 32;
                    break;
                case ArgType.C64BYTE:
                    if (Args[i] is not BBSStrExpr)
                        throw new InvalidDataException(
                            $"Argument {i} should be a string, but it was {Args[i].GetType()}");
                    (Args[i] as BBSStrExpr)!.Length = 64;
                    break;
                case ArgType.C128BYTE:
                    if (Args[i] is not BBSStrExpr)
                        throw new InvalidDataException(
                            $"Argument {i} should be a string, but it was {Args[i].GetType()}");
                    (Args[i] as BBSStrExpr)!.Length = 128;
                    break;
                case ArgType.C256BYTE:
                    if (Args[i] is not BBSStrExpr)
                        throw new InvalidDataException(
                            $"Argument {i} should be a string, but it was {Args[i].GetType()}");
                    (Args[i] as BBSStrExpr)!.Length = 256;
                    break;
                case ArgType.COperand:
                    if (Args[i] is not BBSIntExpr
                        && Args[i] is not BBSHexExpr
                        && Args[i] is not BBSEnumExpr)
                        throw new InvalidDataException(
                            $"Argument {i} should be a COperand, but it was {Args[i].GetType()}");

                    if (Args[i] is BBSIntExpr)
                        Args[i] = new BBSConstExpr((BBSIntExpr)Args[i]);

                    if (Args[i] is BBSHexExpr)
                        Args[i] = new BBSConstExpr((BBSHexExpr)Args[i]);

                    if (Args[i] is BBSEnumExpr)
                        Args[i] = new BBSVarExpr((BBSEnumExpr)Args[i]);
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        foreach (var arg in Args) arg.Compile(context, bigEndian);
    }
}

public class BBSIntExpr : BBS
{
    public int Value { get; set; }

    public BBSIntExpr()
    {
    }

    public BBSIntExpr(int value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"(INT {Value})";
    }

    public override void Compile(CompilerContext context, bool bigEndian = false)
    {
        var output = BitConverter.GetBytes(Value).ToList();
        if (bigEndian) output.Reverse();
        context.Bytecode.AddRange(output);
    }
}

public class BBSHexExpr : BBS
{
    public long Value { get; init; }

    public BBSHexExpr(long value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"(HEX {Value})";
    }

    public override void Compile(CompilerContext context, bool bigEndian = false)
    {
        var output = BitConverter.GetBytes((uint)Value).ToList();
        if (bigEndian) output.Reverse();
        context.Bytecode.AddRange(output);
    }
}

public class BBSBoolExpr : BBS
{
    public bool Value { get; set; }

    public BBSBoolExpr(bool value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"(BOOL {Value})";
    }

    public override void Compile(CompilerContext context, bool bigEndian = false)
    {
        var output = BitConverter.GetBytes(Value ? 1 : 0).ToList();
        if (bigEndian) output.Reverse();
        context.Bytecode.AddRange(output);
    }
}

public class BBSStrExpr : BBS
{
    public string Value { get; init; }
    public int Length { get; set; }

    public BBSStrExpr(string value)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"(STR \"{Value}\")";
    }

    public override void Compile(CompilerContext context, bool bigEndian = false)
    {
        if (Value.Length > Length - 1) throw new InvalidDataException($"The string should be shorter than {Length}!");
        context.Bytecode!.AddRange(Encoding.ASCII.GetBytes(Value));
        for (var i = 0; i < Length - Value.Length; i++) context.Bytecode.Add(0);
    }
}

public class BBSConstExpr : BBSIntExpr
{
    public BBSConstExpr(BBSIntExpr _base)
    {
        Value = _base.Value;
    }
    
    public BBSConstExpr(BBSHexExpr _base)
    {
        Value = (int)_base.Value;
    }

    public override string ToString()
    {
        return $"(CONST {Value})";
    }

    public override void Compile(CompilerContext context, bool bigEndian = false)
    {
        context.Bytecode.AddRange(BitConverter.GetBytes(0).ToList());
        var output = BitConverter.GetBytes(Value).ToList();
        if (bigEndian) output.Reverse();
        context.Bytecode.AddRange(output);
    }
}

public class BBSVarExpr : BBSEnumExpr
{
    public BBSVarExpr(BBSEnumExpr _base)
    {
        Name = _base.Name;
    }

    public override string ToString()
    {
        return $"(VAR {Name})";
    }

    public override void Compile(CompilerContext context, bool bigEndian = false)
    {
        var type = BitConverter.GetBytes(2).ToList();
        if (bigEndian) type.Reverse();
        context.Bytecode.AddRange(type);
        if (BBSConfig.Instance.Variables!.TryGetValue(Name, out var value) || int.TryParse(Name[4..], out value))
        {
            var output = BitConverter.GetBytes(value).ToList();
            if (bigEndian) output.Reverse();
            context.Bytecode.AddRange(output);
        }
        else
            throw new KeyNotFoundException($"Variable {Name} not found!");
    }
}

public class BBSEnumExpr : BBS
{
    public string Name { get; init; }

    public BBSEnumExpr()
    {
        Name = "";
    }

    public BBSEnumExpr(string name)
    {
        Name = name;
    }

    public override string ToString()
    {
        return $"(ENUM {Name})";
    }

    public override void Compile(CompilerContext context, bool bigEndian = false)
    {
        foreach (var @enum in BBSConfig.Instance.Enums?.Values!)
        {
            if (!@enum!.TryGetValue(Name, out var value)) continue;

            var output = BitConverter.GetBytes(value).ToList();
            if (bigEndian) output.Reverse();
            context.Bytecode.AddRange(output);
            return;
        }

        throw new KeyNotFoundException($"Enum value {Name} not found!");
    }
}