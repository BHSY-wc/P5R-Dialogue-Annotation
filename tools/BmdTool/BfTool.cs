using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.FlowScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage;

class BfTool
{
    const string ModDir = @"D:/downlode/P5RMod/ReLoad2/Mods/reloaded.universal.fileemulationframework.bmd";

    static void Init()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var name = new System.Reflection.AssemblyName(e.Name).Name;
            var p = Path.Combine(ModDir, name + ".dll");
            return File.Exists(p) ? System.Reflection.Assembly.LoadFrom(p) : null;
        };
        AtlusEncoding.SetCharsetDirectory(Path.Combine(ModDir, "Charsets"));
    }

    static string Tokens(TokenText tt)
    {
        if (tt == null) return "";
        var sb = new StringBuilder();
        foreach (var t in tt.Tokens)
        {
            if (t is StringToken st) sb.Append(st.Value);
            else if (t is NewLineToken) sb.Append(" / ");
            else if (t is FunctionToken ft) sb.Append("[f]");
        }
        return sb.ToString();
    }

    public static string MsgText(Dialog d)
    {
        if (d is SelectionDialog sd) return "SEL[" + string.Join(" || ", sd.Options.Select(Tokens)) + "]";
        if (d is MessageDialog md) return string.Join(" / ", (md.Lines ?? new System.Collections.Generic.List<TokenText>()).Select(Tokens));
        return "(" + d.GetType().Name + ")";
    }

    public static string RawDump(string src, bool withFlow)
    {
        Init();
        var fs = FlowScript.FromFile(src, AtlusEncoding.Persona5RoyalCHS);
        var sb = new StringBuilder();
        sb.AppendLine($"# {Path.GetFileName(src)} ver={fs.FormatVersion} procs={fs.Procedures.Count} msgs={fs.MessageScript?.Dialogs.Count}");
        if (fs.MessageScript != null)
        {
            foreach (var d in fs.MessageScript.Dialogs)
            {
                if (d is SelectionDialog sd)
                    sb.AppendLine($"MSG {d.Name}: SEL[{string.Join(" || ", sd.Options.Select(Tokens))}]");
                else if (d is MessageDialog md)
                    sb.AppendLine($"MSG {d.Name}: {string.Join(" / ", (md.Lines ?? new System.Collections.Generic.List<TokenText>()).Select(Tokens))}");
                else sb.AppendLine($"MSG {d.Name}: ({d.GetType().Name})");
            }
        }
        if (withFlow)
        {
            foreach (var proc in fs.Procedures)
            {
                sb.AppendLine($"PROC {proc.Name}");
                int i = 0;
                foreach (var ins in proc.Instructions)
                {
                    sb.Append("  ").Append(i).Append(": ").Append(ins.Opcode);
                    try { if (ins.Operand != null) sb.Append(' ').Append(ins.Operand); } catch { }
                    sb.AppendLine();
                    i++;
                }
            }
        }
        return sb.ToString();
    }
}
