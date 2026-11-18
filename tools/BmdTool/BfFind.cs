using System;
using System.IO;
using System.Linq;
using System.Text;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.FlowScriptLanguage;

class BfFind
{
    const string ModDir = @"D:/downlode/P5RMod/ReLoad2/Mods/reloaded.universal.fileemulationframework.bmd";
    static bool _init;

    static void Init()
    {
        if (_init) return;
        _init = true;
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var name = new System.Reflection.AssemblyName(e.Name).Name;
            var p = Path.Combine(ModDir, name + ".dll");
            return File.Exists(p) ? System.Reflection.Assembly.LoadFrom(p) : null;
        };
        AtlusEncoding.SetCharsetDirectory(Path.Combine(ModDir, "Charsets"));
    }

    public static void Run(string root, string outFile, string[] kws)
    {
        Init();
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                             .Where(f => f.ToUpper().EndsWith(".BF")).OrderBy(f => f).ToList();
        using var w = new StreamWriter(outFile, false, new UTF8Encoding(false));
        int hit = 0, scanned = 0, failed = 0, withMsgs = 0;
        foreach (var f in files)
        {
            try
            {
                var fs = FlowScript.FromFile(f, AtlusEncoding.Persona5RoyalCHS);
                scanned++;
                if (fs.MessageScript == null) continue;
                int n = fs.MessageScript.Dialogs.Count;
                if (n > 1) withMsgs++;
                var hits = new System.Collections.Generic.List<string>();
                foreach (var d in fs.MessageScript.Dialogs)
                {
                    string t = BfTool.MsgText(d);
                    if (kws.Any(k => t.Contains(k))) hits.Add(d.Name + ": " + t);
                }
                if (hits.Count > 0)
                {
                    hit++;
                    w.WriteLine("### " + Path.GetRelativePath(root, f).Replace('\\','/') + $"  (msgs={n}, hits={hits.Count})");
                    foreach (var h in hits.Take(30)) w.WriteLine("   " + h.Replace("\n"," "));
                    w.WriteLine();
                }
            }
            catch { failed++; }
        }
        w.WriteLine($"# scanned={scanned} withMsgs>1={withMsgs} hitFiles={hit} failed={failed}");
        Console.WriteLine($"[bffind] files={files.Count} scanned={scanned} withMsgs>1={withMsgs} hitFiles={hit} failed={failed}");
    }
}
