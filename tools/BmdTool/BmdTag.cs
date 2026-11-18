using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.MessageScriptLanguage;

class BmdTag
{
    // 判定"最佳选项"的标记：FunctionToken index 8（参数固定 2,1,2217）
    static bool IsBest(TokenText tt)
    {
        foreach (var t in tt.Tokens)
            if (t is FunctionToken ft && ft.FunctionIndex == 8) return true;
        return false;
    }

    // 通用：某个功能 token（可限定参数）是否存在
    static bool HasFunc(TokenText tt, int funcIdx, string args)
    {
        foreach (var t in tt.Tokens)
        {
            if (!(t is FunctionToken ft) || ft.FunctionIndex != funcIdx) continue;
            if (args == null) return true;
            try { if (string.Join(",", ft.Arguments) == args) return true; } catch { }
        }
        return false;
    }

    // 课堂答题正确答案：<f 7 | 0,0,25,1>
    static bool IsClassAnswer(TokenText tt) => HasFunc(tt, 7, "0,0,25,1");

    static bool AppendMark(TokenText toks, string mark)
    {
        var list = toks.Tokens as IList<Token>;
        if (list == null) return false;
        int i = -1;
        for (int k = 0; k < list.Count; k++) if (list[k] is StringToken) i = k;
        if (i < 0) return false;
        var st = (StringToken)list[i];
        if (st.Value.EndsWith(mark)) return false;
        list[i] = new StringToken(st.Value + mark);
        return true;
    }

    // 双标记：好感(f8) -> m8，课堂答案(f7:0,0,25,1) -> m7
    static (int, int) TagDual(MessageScript script, string m8, string m7)
    {
        int a = 0, b = 0;
        foreach (var d in script.Dialogs)
        {
            if (!(d is SelectionDialog sd)) continue;
            foreach (var opt in sd.Options)
            {
                if (HasFunc(opt, 8, null)) { if (AppendMark(opt, m8)) a++; }
                else if (HasFunc(opt, 7, "0,0,25,1")) { if (AppendMark(opt, m7)) b++; }
            }
        }
        return (a, b);
    }

    public static void RunDual(string srcRoot, string dstRoot, string m8, string m7)
    {
        var files = Directory.EnumerateFiles(srcRoot, "*", SearchOption.AllDirectories)
                             .Where(f => f.ToUpper().EndsWith(".BMD")).OrderBy(f => f).ToList();
        int ok = 0, skip = 0, fail = 0, c8 = 0, c7 = 0;
        foreach (var f in files)
        {
            var rel = Path.GetRelativePath(srcRoot, f);
            try
            {
                var script = MessageScript.FromFile(f, FormatVersion.Detect, AtlusEncoding.Persona5RoyalCHS);
                var (a, b) = TagDual(script, m8, m7);
                if (a == 0 && b == 0) { skip++; continue; }
                var outp = Path.Combine(dstRoot, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(outp));
                script.ToFile(outp);
                ok++; c8 += a; c7 += b;
            }
            catch (Exception ex) { fail++; Console.WriteLine($"[fail] {rel}: {ex.Message}"); }
        }
        Console.WriteLine($"[bmddual] files={ok} skipped={skip} failed={fail} coop♡={c8} param★={c7} -> {dstRoot}");
    }

    static bool TagOne(MessageScript script, string mark)
    {
        int n = 0;
        foreach (var d in script.Dialogs)
        {
            if (!(d is SelectionDialog sd)) continue;
            foreach (var opt in sd.Options)
            {
                if (!IsBest(opt)) continue;
                var toks = opt.Tokens;
                int i = toks.FindLastIndex(t => t is StringToken);
                if (i < 0) continue;
                var st = (StringToken)toks[i];
                if (st.Value.EndsWith(mark)) continue;
                toks[i] = new StringToken(st.Value + mark);
                n++;
            }
        }
        return n > 0;
    }

    public static void Run(string src, string dst, string mark)
    {
        var script = MessageScript.FromFile(src, FormatVersion.Detect, AtlusEncoding.Persona5RoyalCHS);
        bool changed = TagOne(script, mark);
        if (!changed) { Console.WriteLine($"[tag] {Path.GetFileName(src)} 无标记选项，跳过"); return; }
        script.ToFile(dst);
        Console.WriteLine($"[tag] {Path.GetFileName(src)} -> {dst}");
    }

    // 批量：保持相对目录结构输出
    public static void RunDir(string srcRoot, string dstRoot, string mark, string filter = null)
    {
        var files = Directory.EnumerateFiles(srcRoot, "*", SearchOption.AllDirectories)
                             .Where(f => f.ToUpper().EndsWith(".BMD")).OrderBy(f => f).ToList();
        int ok = 0, skip = 0, fail = 0;
        foreach (var f in files)
        {
            var rel = Path.GetRelativePath(srcRoot, f);
            if (!string.IsNullOrEmpty(filter) && !Path.GetFileName(f).Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                var script = MessageScript.FromFile(f, FormatVersion.Detect, AtlusEncoding.Persona5RoyalCHS);
                if (!TagOne(script, mark)) { skip++; continue; }
                var outp = Path.Combine(dstRoot, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(outp));
                script.ToFile(outp);
                ok++;
            }
            catch (Exception ex) { fail++; Console.WriteLine($"[fail] {rel}: {ex.Message}"); }
        }
        Console.WriteLine($"[tagdir] tagged={ok} skipped={skip} failed={fail} -> {dstRoot}");
    }
}
