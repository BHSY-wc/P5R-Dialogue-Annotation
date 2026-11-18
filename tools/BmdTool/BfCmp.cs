using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.FlowScriptLanguage;

class BfCmp
{
    const string ModDir = @"D:/downlode/P5RMod/ReLoad2/Mods/reloaded.universal.fileemulationframework.bmd";
    static bool _init;
    static void Init()
    {
        if (_init) return; _init = true;
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var n = new System.Reflection.AssemblyName(e.Name).Name;
            var p = Path.Combine(ModDir, n + ".dll");
            return File.Exists(p) ? System.Reflection.Assembly.LoadFrom(p) : null;
        };
        AtlusEncoding.SetCharsetDirectory(Path.Combine(ModDir, "Charsets"));
    }

    static List<(string op, int target)> Sig(string path)
    {
        Init();
        var fs = FlowScript.FromFile(path, AtlusEncoding.Persona5RoyalCHS);
        var list = new List<(string, int)>();
        foreach (var proc in fs.Procedures)
        {
            int i = 0;
            foreach (var ins in proc.Instructions)
            {
                int target = -1;
                var op = ins.Operand;
                if (op != null)
                {
                    var t = op.GetType();
                    if (t.IsPrimitive) { try { target = Convert.ToInt32(op); } catch { } }
                    else
                    {
                        var np = t.GetProperty("Value") ?? t.GetProperty("Index");
                        if (np != null) { try { target = Convert.ToInt32(np.GetValue(op)); } catch { } }
                    }
                }
                list.Add((proc.Name + ":" + i + ":" + ins.Opcode + ":" + t2(ins), target));
                i++;
            }
        }
        return list;

        string t2(Instruction ins)
        {
            if (ins.Operand == null) return "-";
            return ins.Operand.GetType().Name;
        }
    }

    public static void Run(string a, string b)
    {
        var sa = Sig(a);
        var sb = Sig(b);
        Console.WriteLine($"原: {sa.Count} 条指令, 新: {sb.Count} 条");
        int diff = 0;
        for (int i = 0; i < Math.Min(sa.Count, sb.Count); i++)
        {
            if (sa[i].op != sb[i].op || sa[i].target != sb[i].target)
            {
                if (diff < 12) Console.WriteLine($"#{i}\n  原: {sa[i].op} target={sa[i].target}\n  新: {sb[i].op} target={sb[i].target}");
                diff++;
            }
        }
        Console.WriteLine($"差异条数: {diff} / {Math.Min(sa.Count, sb.Count)}");
    }
}
