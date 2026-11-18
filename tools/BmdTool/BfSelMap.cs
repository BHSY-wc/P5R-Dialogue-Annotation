using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.FlowScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage;

class BfSelMap
{
    static string TokText(TokenText tt)
    {
        if (tt == null) return "";
        var sb = new StringBuilder();
        foreach (var t in tt.Tokens)
        {
            if (t is StringToken st) sb.Append(st.Value);
            else if (t is NewLineToken) sb.Append(" / ");
        }
        return sb.ToString();
    }

    // 分析单个文件里的所有 SEL，输出每个选项分支是否含加好感调用
    public static void Analyze(string src, TextWriter w)
    {
        var fs = FlowScript.FromFile(src, AtlusEncoding.Persona5RoyalCHS);
        var dialogs = fs.MessageScript?.Dialogs;
        w.WriteLine($"### {Path.GetFileName(src)}");
        foreach (var proc in fs.Procedures)
        {
            var ins = proc.Instructions.ToList();
            for (int i = 0; i < ins.Count; i++)
            {
                if (ins[i].Opcode != Opcode.COMM) continue;
                int comm = BfDis.OperandOf(ins[i]);
                if (!BfDis.IsSel(comm)) continue;

                int selParam = -1;
                for (int k = i - 1; k >= Math.Max(0, i - 4); k--)
                    if (ins[k].Opcode == Opcode.PUSHIS) { selParam = BfDis.OperandOf(ins[k]); break; }

                SelectionDialog sd = null;
                if (dialogs != null && selParam >= 0 && selParam < dialogs.Count && dialogs[selParam] is SelectionDialog s) sd = s;

                // 找紧随其后的 POPLIX var
                int var = -1;
                int j = i + 1;
                for (; j < Math.Min(ins.Count, i + 4); j++)
                    if (ins[j].Opcode == Opcode.POPLIX) { var = BfDis.OperandOf(ins[j]); break; }
                if (var < 0) continue;

                // 收集 PUSHIS k; PUSHLIX var; EQ; IF lbl 模式形成分支
                var branches = new List<(int opt, int bodyStart, int bodyEnd)>();
                int p = j + 1;
                while (p + 3 < ins.Count)
                {
                    if (ins[p].Opcode == Opcode.PUSHIS && BfDis.OperandOf(ins[p]) >= 0
                        && ins[p + 1].Opcode == Opcode.PUSHLIX && BfDis.OperandOf(ins[p + 1]) == var
                        && ins[p + 2].Opcode == Opcode.EQ && ins[p + 3].Opcode == Opcode.IF)
                    {
                        int opt = BfDis.OperandOf(ins[p]);
                        int start = p + 4;
                        // 分支体到下一个同类模式或 GOTO 为止
                        int end = start;
                        while (end < ins.Count)
                        {
                            if (ins[end].Opcode == Opcode.GOTO) { end++; break; }
                            if (end + 3 < ins.Count && ins[end].Opcode == Opcode.PUSHIS
                                && ins[end + 1].Opcode == Opcode.PUSHLIX && BfDis.OperandOf(ins[end + 1]) == var
                                && ins[end + 2].Opcode == Opcode.EQ && ins[end + 3].Opcode == Opcode.IF) break;
                            if (ins[end].Opcode == Opcode.END) break;
                            end++;
                        }
                        branches.Add((opt, start, end));
                        p = end;
                        continue;
                    }
                    if (ins[p].Opcode == Opcode.END) break;
                    p++;
                }
                if (branches.Count == 0) continue;

                w.WriteLine($"  PROC {proc.Name}  SEL#{selParam} ({sd?.Name ?? "?"}) options={sd?.Options.Count}");
                int best = -1, bestPt = -1;
                var pts = new Dictionary<int, int>();
                foreach (var (opt, bs, be) in branches)
                {
                    int add = 0, total = 0;
                    for (int k = bs; k < be && k < ins.Count; k++)
                    {
                        if (ins[k].Opcode != Opcode.COMM) continue;
                        int c = BfDis.OperandOf(ins[k]);
                        total++;
                        if (BfDis.IsAddPoint(c)) add++;
                        else if (BfDis.Name(c) == "CMM_FRIEND" || BfDis.Name(c) == "CMM_LVUP") add++;
                    }
                    pts[opt] = add;
                    if (add > bestPt) { bestPt = add; best = opt; }
                    var txt = (sd != null && opt < sd.Options.Count) ? TokText(sd.Options[opt]) : "?";
                    w.WriteLine($"    opt{opt}: {(add > 0 ? "★加点x" + add : "      ")}  \"{txt}\"");
                }
                w.WriteLine($"    => 最佳选项 opt{best}");
            }
        }
    }

    public static void RunAll(string root, string outFile)
    {
        BfDis.LoadFuncs();
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                             .Where(f => f.ToUpper().EndsWith(".BF")).OrderBy(f => f).ToList();
        using var w = new StreamWriter(outFile, false, new UTF8Encoding(false));
        int ok = 0, fail = 0, secs = 0;
        foreach (var f in files)
        {
            try
            {
                var sw = new StringWriter();
                Analyze(f, sw);
                var t = sw.ToString();
                if (t.Contains("★加点"))
                {
                    secs++;
                    w.WriteLine($"=== {Path.GetRelativePath(root, f).Replace('\\', '/')}");
                    w.Write(t);
                }
                ok++;
            }
            catch { fail++; }
        }
        Console.WriteLine($"[bfselmap] ok={ok} fail={fail} withAddPointSel={secs} -> {outFile}");
    }
}
