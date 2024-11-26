using System.Text;
using BBScript.Config;
using BBScript.Language;

namespace BBScript.Decompiler;

public static class BBSDecompiler
{
    public static string Decompile(byte[] bytecode)
    {
        var output = new StringBuilder();
        
        var jumpTableSize = BitConverter.ToInt32(bytecode);

        var pos = jumpTableSize * 0x24 + 0x4;

        var indentLevel = 0;

        while (pos < bytecode.Length)
        {
            var id = BitConverter.ToInt32(bytecode, pos);
            pos += 4;
            
            if (!BBSConfig.Instance.Instructions!.TryGetValue(id, out Instruction? value))
                throw new KeyNotFoundException($"Instruction {id} not found!");

            var expressions = new List<string>();

            if (value.Args != null)
                foreach (var arg in value.Args)
                {
                    switch (arg.Type)
                    {
                        case ArgType.BOOL:
                        case ArgType.S8:
                        case ArgType.S16:
                        case ArgType.S32:
                        {
                            var val = BitConverter.ToInt32(bytecode, pos);
                            expressions.Add(val.ToString());
                            pos += 4;
                            break;
                        }
                        case ArgType.U8:
                        case ArgType.U16:
                        case ArgType.U32:
                        {
                            var val = BitConverter.ToUInt32(bytecode, pos);
                            expressions.Add($"0x{val}");
                            pos += 4;
                            break;
                        }
                        case ArgType.Enum:
                        {
                            var val = BitConverter.ToInt32(bytecode, pos);
                            if (BBSConfig.Instance.Enums![arg.EnumName!]!.ContainsValue(val))
                            {
                                expressions.Add(BBSConfig.Instance.Enums![arg.EnumName!]!
                                    .First(x => x.Value == val).Key);
                            }
                            else
                            {
                                expressions.Add(val.ToString());
                            }
                            pos += 4;
                            break;
                        }
                        case ArgType.C16BYTE:
                        {
                            var val = "\"" + Encoding.ASCII.GetString(bytecode, pos, 16) + "\"";
                            val = val.Replace("\0", string.Empty);
                            expressions.Add(val);
                            pos += 16;
                            break;
                        }
                        case ArgType.C32BYTE:
                        {
                            var val = "\"" + Encoding.ASCII.GetString(bytecode, pos, 32) + "\"";
                            val = val.Replace("\0", string.Empty);
                            expressions.Add(val);
                            pos += 32;
                            break;
                        }
                        case ArgType.C64BYTE:
                        {
                            var val = "\"" + Encoding.ASCII.GetString(bytecode, pos, 64) + "\"";
                            val = val.Replace("\0", string.Empty);
                            expressions.Add(val);
                            pos += 64;
                            break;
                        }
                        case ArgType.C128BYTE:
                        {
                            var val = "\"" + Encoding.ASCII.GetString(bytecode, pos, 128) + "\"";
                            val = val.Replace("\0", string.Empty);
                            expressions.Add(val);
                            pos += 128;
                            break;
                        }
                        case ArgType.C256BYTE:
                        {
                            var val = "\"" + Encoding.ASCII.GetString(bytecode, pos, 256) + "\"";
                            val = val.Replace("\0", string.Empty);
                            expressions.Add(val);
                            pos += 256;
                            break;
                        }
                        case ArgType.COperand:
                        {
                            var type = BitConverter.ToInt32(bytecode, pos);
                            pos += 4;
                            var val = BitConverter.ToInt32(bytecode, pos);
                            if (type == 0)
                            {
                                expressions.Add($"Const({val})");
                            }
                            else
                            {
                                string name;
                                if (BBSConfig.Instance.Variables!.ContainsValue(val))
                                {
                                    name = BBSConfig.Instance.Variables!
                                        .First(x => x.Value == val).Key;
                                }
                                else
                                {
                                    name = val.ToString();
                                }
                                expressions.Add($"Var({name})");
                            }
                            pos += 4;
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

            switch (value.Indent)
            {
                case IndentType.End:
                    indentLevel--;
                    break;
                case IndentType.ScopeEnd:
                    indentLevel = 0;
                    break;
                case IndentType.Cell:
                default:
                    break;
            }

            if (indentLevel < 0) indentLevel = 0;

            for (var i = 0; i < indentLevel; i++)
            {
                output.Append('\t');
            }

            if (value.Name != null)
            {
                output.Append(value.Name + "(");
            }
            else
            {
                output.Append("Unknown" + id + "(");
            }
            
            for (var i = 0; i < expressions.Count; i++)
            {
                output.Append(expressions[i]);
                if (i < expressions.Count - 1) output.Append(", ");
            }
            output.Append(");\n");

            switch (value.Indent)
            {
                case IndentType.Begin:
                    indentLevel++;
                    break;
                case IndentType.ScopeBegin:
                    indentLevel = 1;
                    break;
                case IndentType.Cell:
                    indentLevel = 2;
                    break;
                case IndentType.ScopeEnd:
                    output.Append('\n');
                    break;
            }
        }
        
        return output.ToString();
    }
}