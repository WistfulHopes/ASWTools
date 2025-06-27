using System.Text;
using BBScript.Config;

namespace BBScript.Decompiler;

public static class BBSDecompiler
{
    public static void Decompile(BinaryReader reader, StreamWriter writer, bool bigEndian = false)
    {
        var jumpTableSize = bigEndian ? reader.ReadInt32BE() : bigEndian ? reader.ReadInt32BE() : reader.ReadInt32();

        reader.BaseStream.Position = jumpTableSize * 0x24 + 0x4;

        var indentLevel = 0;

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var id = bigEndian ? reader.ReadInt32BE() : bigEndian ? reader.ReadInt32BE() : reader.ReadInt32();

            if (!BBSConfig.Instance.Instructions!.TryGetValue(id, out Instruction? value))
                throw new KeyNotFoundException($"Instruction {id} not found!");

            var expressions = new List<string>();

            if (value.Args != null)
                foreach (var arg in value.Args)
                {
                    switch (arg.Type)
                    {
                        case ArgType.BOOL:
                        {
                            var val = bigEndian ? reader.ReadInt32BE() : reader.ReadInt32();
                            expressions.Add(val > 0 ? "true" : "false");
                            break;
                        }
                        case ArgType.S8:
                        case ArgType.S16:
                        case ArgType.S32:
                        {
                            var val = bigEndian ? reader.ReadInt32BE() : reader.ReadInt32();
                            expressions.Add(val.ToString());
                            break;
                        }
                        case ArgType.U8:
                        case ArgType.U16:
                        case ArgType.U32:
                        {
                            var val = bigEndian ? reader.ReadUInt32BE() : reader.ReadUInt32();
                            expressions.Add($"0x{val:X}");
                            break;
                        }
                        case ArgType.Enum:
                        {
                            var val = bigEndian ? reader.ReadInt32BE() : reader.ReadInt32();
                            if (BBSConfig.Instance.Enums![arg.EnumName!]!.ContainsValue(val))
                            {
                                expressions.Add(BBSConfig.Instance.Enums![arg.EnumName!]!
                                    .First(x => x.Value == val).Key);
                            }
                            else
                            {
                                expressions.Add(val.ToString());
                            }
                            break;
                        }
                        case ArgType.C16BYTE:
                        {
                            var val = "\"" + Encoding.ASCII.GetString(reader.ReadBytes(16)) + "\"";
                            val = val.Replace("\0", string.Empty);
                            expressions.Add(val);
                            break;
                        }
                        case ArgType.C32BYTE:
                        {
                            var val = "\"" + Encoding.ASCII.GetString(reader.ReadBytes(32)) + "\"";
                            val = val.Replace("\0", string.Empty);
                            expressions.Add(val);
                            break;
                        }
                        case ArgType.C64BYTE:
                        {
                            var val = "\"" + Encoding.ASCII.GetString(reader.ReadBytes(64)) + "\"";
                            val = val.Replace("\0", string.Empty);
                            expressions.Add(val);
                            break;
                        }
                        case ArgType.C128BYTE:
                        {
                            var val = "\"" + Encoding.ASCII.GetString(reader.ReadBytes(128)) + "\"";
                            val = val.Replace("\0", string.Empty);
                            expressions.Add(val);
                            break;
                        }
                        case ArgType.C256BYTE:
                        {
                            var val = "\"" + Encoding.ASCII.GetString(reader.ReadBytes(256)) + "\"";
                            val = val.Replace("\0", string.Empty);
                            expressions.Add(val);
                            break;
                        }
                        case ArgType.COperand:
                        {
                            var type = bigEndian ? reader.ReadInt32BE() : reader.ReadInt32();
                            var val = bigEndian ? reader.ReadInt32BE() : reader.ReadInt32();
                            if (type == 0)
                            {
                                expressions.Add($"{val}");
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
                                    name = "VAR_" + val;
                                }
                                expressions.Add($"{name}");
                            }
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
                    indentLevel = 1;
                    break;
                case IndentType.CellEnd:
                    indentLevel = 1;
                    break;
                default:
                    break;
            }

            if (indentLevel < 0) indentLevel = 0;

            for (var i = 0; i < indentLevel; i++)
            {
                writer.Write('\t');
            }

            if (value.Name != null)
            {
                writer.Write(value.Name + "(");
            }
            else
            {
                writer.Write("Unknown" + id + "(");
            }
            
            for (var i = 0; i < expressions.Count; i++)
            {
                writer.Write(expressions[i]);
                if (i < expressions.Count - 1) writer.Write(", ");
            }
            writer.Write(");\n");

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
                    writer.Write('\n');
                    break;
                case IndentType.CellEnd:
                    indentLevel = 1;
                    break;
            }
        }
    }
}