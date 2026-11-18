using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.MessageScriptLanguage;

class BmdTok
{
    public static void Dump(string src, TextWriter w)
    {
        var script = MessageScript.FromFile(src, FormatVersion.Detect, AtlusEncoding.Persona5RoyalCHS);
        w.WriteLine($"# {Path.GetFileName(src)} dialogs={script.Dialogs.Count}");
        foreach (var d in script.Dialogs)
        {
            if (!(d is SelectionDialog sd)) continue;
            w.WriteLine($"SEL {d.Name} opts={sd.Options.Count}");
            for (int i = 0; i < sd.Options.Count; i++)
            {
                var sb = new StringBuilder();
                foreach (var t in sd.Options[i].Tokens)
                {
                    if (t is StringToken st) sb.Append(st.Value);
                    else if (t is NewLineToken) sb.Append("<NL>");
                    else if (t is FunctionToken ft)
                    {
                        sb.Append("<f ").Append(ft.FunctionIndex);
                        try { sb.Append(" | ").Append(string.Join(",", ft.Arguments)); } catch { }
                        sb.Append('>');
                    }
                    else sb.Append('<').Append(t.GetType().Name).Append('>');
                }
                w.WriteLine($"  [{i}] {sb}");
            }
        }
    }

    public static void RunAll(string root, string outFile)
    {
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                             .Where(f => f.ToUpper().EndsWith(".BMD")).OrderBy(f => f).ToList();
        using var w = new StreamWriter(outFile, false, new UTF8Encoding(false));
        int ok = 0, fail = 0;
        foreach (var f in files)
        {
            try
            {
                var sw = new StringWriter();
                Dump(f, sw);
                var t = sw.ToString();
                if (t.Contains("SEL ")) { w.WriteLine($"### {Path.GetRelativePath(root, f).Replace('\\', '/')}"); w.Write(t); }
                ok++;
            }
            catch { fail++; }
        }
        Console.WriteLine($"[bmdtok] ok={ok} fail={fail} -> {outFile}");
    }
}
