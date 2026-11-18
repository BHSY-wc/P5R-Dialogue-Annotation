using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.MessageScriptLanguage;

// 校验：标注后的 BMD 去掉标记字符后，与原文件的全部 dialog/行/token 是否完全一致
class BmdVerify
{
    static string Tok(TokenText tt, string strip)
    {
        var sb = new StringBuilder();
        if (tt == null) return "<null>";
        foreach (var t in tt.Tokens)
        {
            if (t is StringToken st)
            {
                var v = st.Value;
                // strip 中的任意字符视为标注符号：跳过末尾空白后，若那个字符是符号就去掉它
                if (strip != null && v.Length > 0)
                {
                    int i = v.Length - 1;
                    while (i >= 0 && char.IsWhiteSpace(v[i])) i--;
                    if (i >= 0 && strip.IndexOf(v[i]) >= 0) v = v.Remove(i, 1);
                }
                sb.Append(v);
            }
            else if (t is NewLineToken) sb.Append("<NL>");
            else if (t is FunctionToken ft)
            {
                sb.Append("<f ").Append(ft.FunctionIndex);
                try { sb.Append(" | ").Append(string.Join(",", ft.Arguments)); } catch { }
                sb.Append('>');
            }
            else sb.Append('<').Append(t.GetType().Name).Append('>');
        }
        return sb.ToString();
    }

    static string Ser(MessageScript s, string strip)
    {
        var sb = new StringBuilder();
        sb.Append("N=").Append(s.Dialogs.Count).Append('\n');
        foreach (var d in s.Dialogs)
        {
            sb.Append("D|").Append(d.Name).Append('|').Append(d.GetType().Name).Append('\n');
            if (d is MessageDialog md)
            {
                try { if (md.Speaker != null) sb.Append("SPK|").Append(md.Speaker.ToString()).Append('\n'); } catch { }
                if (md.Lines != null)
                    foreach (var ln in md.Lines) sb.Append("L|").Append(Tok(ln, strip)).Append('\n');
            }
            else if (d is SelectionDialog sd)
            {
                sb.Append("TOP|-\n");
                foreach (var o in sd.Options) sb.Append("O|").Append(Tok(o, strip)).Append('\n');
            }
        }
        return sb.ToString();
    }

    static MessageScript Load(string p) =>
        MessageScript.FromFile(p, FormatVersion.Detect, AtlusEncoding.Persona5RoyalCHS);

    public static void RunDir(string srcRoot, string dstRoot, string mark)
    {
        var files = Directory.EnumerateFiles(dstRoot, "*", SearchOption.AllDirectories)
                             .Where(f => f.ToUpper().EndsWith(".BMD")).OrderBy(f => f).ToList();
        int same = 0, diff = 0, err = 0;
        foreach (var dst in files)
        {
            var rel = Path.GetRelativePath(dstRoot, dst);
            var src = Path.Combine(srcRoot, rel);
            if (!File.Exists(src)) { Console.WriteLine($"[nostart] {rel}"); err++; continue; }
            try
            {
                var a = Ser(Load(src), null);
                var b = Ser(Load(dst), mark);
                if (a == b) { same++; continue; }
                diff++;
                Console.WriteLine($"[DIFF] {rel}");
                var la = a.Split('\n'); var lb = b.Split('\n');
                int lim = Math.Min(la.Length, lb.Length);
                for (int i = 0; i < lim; i++)
                    if (la[i] != lb[i]) { Console.WriteLine($"   line{i}: src={Trunc(la[i])} | dst={Trunc(lb[i])}"); break; }
                if (la.Length != lb.Length) Console.WriteLine($"   count: src={la.Length} dst={lb.Length}");
            }
            catch (Exception ex) { err++; Console.WriteLine($"[ERROR] {rel}: {ex.Message}"); }
        }
        Console.WriteLine($"[verify] files={files.Count} same={same} diff={diff} error={err}");
    }

    static string Trunc(string s) => s.Length <= 80 ? s : s.Substring(0, 80) + "…";
}
