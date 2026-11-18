using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Compiler;
using AtlusScriptLibrary.MessageScriptLanguage.Decompiler;

class Program
{
    const string ModDir = @"D:/downlode/P5RMod/ReLoad2/Mods/reloaded.universal.fileemulationframework.bmd";

    static string OptText(TokenText tt)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var t in tt.Tokens)
        {
            if (t is StringToken st) sb.Append(st.Value);
            else if (t is NewLineToken) sb.Append(" / ");
            else if (t is FunctionToken ft) sb.Append("[f]");
        }
        return sb.ToString();
    }

    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var name = new AssemblyName(e.Name).Name;
            var p = Path.Combine(ModDir, name + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };
        AtlusEncoding.SetCharsetDirectory(Path.Combine(ModDir, "Charsets"));

        if (args[0] == "bfdump") { File.WriteAllText(args[2], BfTool.RawDump(args[1], true), new System.Text.UTF8Encoding(false)); Console.WriteLine("[bfdump] ok -> " + args[2]); return; }
        if (args[0] == "bfmsg") { File.WriteAllText(args[2], BfTool.RawDump(args[1], true).Split(new[]{"PROC "},StringSplitOptions.None)[0], new System.Text.UTF8Encoding(false)); Console.WriteLine("[bfmsg] ok"); return; }
        if (args[0] == "libprobe") { LibProbe.Run(); return; }
        if (args[0] == "bffind") { BfFind.Run(args[1], args[2], args.Skip(3).ToArray()); return; }
        if (args[0] == "bfcmp") { BfCmp.Run(args[1], args[2]); return; }
        if (args[0] == "bfmsgdump") { BfMsg.Dump(args[1], args[2]); return; }
        if (args[0] == "bfmerge") { BfMsg.Merge(args[1], args[2], args[3]); return; }
        if (args[0] == "bfrt") { BfEdit.RoundTrip(args[1], args[2]); return; }
        if (args[0] == "bfinfo") { BfInfo.Run(args[1]); return; }
        if (args[0] == "bfselmark") { BfEdit.MarkSel(args[1], args[2], args[3]); return; }
        if (args[0] == "bfstar") { BfEdit.StarOptions(args[1], args[2], args[3], args[4], args.Skip(5).ToArray(), true); return; }
        if (args[0] == "bfprobe") { BfProbe.Run(); return; }
        if (args[0] == "bfcommstat") { BfDis.CommStat(args[1], args[2]); return; }
        if (args[0] == "bfselstat") { BfDis.SelStat(args[1], args[2]); return; }
        if (args[0] == "bmddual") { BmdTag.RunDual(args[1], args[2], args[3], args[4]); return; }
        if (args[0] == "bmdromance")
        {
            // bmdromance <srcRoot> <dstRoot> <恋人符号> <友人符号> <好感符号> <答案符号> [恋爱标志位,逗号分隔]
            BmdRomance.RunDir(args[1], args[2], args[3], args[4], args[5], args[6],
                              args.Length > 7 ? args[7] : null);
            return;
        }
        if (args[0] == "bmdtag")
        {
            // bmdtag <src> <dst> <mark> [filter]
            if (System.IO.Directory.Exists(args[1])) BmdTag.RunDir(args[1], args[2], args[3], args.Length > 4 ? args[4] : null);
            else BmdTag.Run(args[1], args[2], args[3]);
            return;
        }
        if (args[0] == "bmdtok")
        {
            if (System.IO.Directory.Exists(args[1])) BmdTok.RunAll(args[1], args[2]);
            else using (var w = new System.IO.StreamWriter(args[2], false, new System.Text.UTF8Encoding(false))) BmdTok.Dump(args[1], w);
            return;
        }
        if (args[0] == "bfparam") { BfDis.ParamStat(args[1], args[2]); return; }
        if (args[0] == "bmdverify") { BmdVerify.RunDir(args[1], args[2], args[3]); return; }
        if (args[0] == "bfselmap") { BfSelMap.RunAll(args[1], args[2]); return; }
        if (args[0] == "bfsel1") { using (var w = new System.IO.StreamWriter(args[2], false, new System.Text.UTF8Encoding(false))) BfSelMap.Analyze(args[1], w); return; }
        if (args[0] == "bfdis") { File.WriteAllText(args[2], BfDis.Disasm(args[1]), new System.Text.UTF8Encoding(false)); Console.WriteLine("[bfdis] ok -> " + args[2]); return; }
        if (args[0] == "bfdecomp") { BfDecomp.Run(args[1], args[2]); return; }
        if (args[0] == "apiprobe") { ApiProbe.Run(args[1]); return; }
        var mode = args[0];
        var src = args[1];

        if (mode == "dump")
        {
            var script = MessageScript.FromFile(src, FormatVersion.Detect, AtlusEncoding.Persona5RoyalCHS);
            var sw = new StringWriter();
            using (var dec = new MessageScriptDecompiler(sw)) dec.Decompile(script);
            File.WriteAllText(args[2], sw.ToString(), new System.Text.UTF8Encoding(false));
            Console.WriteLine($"[dump] {script.Dialogs.Count} dialogs -> {args[2]}");
        }
        else if (mode == "build")
        {
            var text = File.ReadAllText(src);
            var compiler = new MessageScriptCompiler(FormatVersion.Version1, AtlusEncoding.Persona5RoyalCHS);
            var script = compiler.Compile(text);
            script.ToFile(args[2]);
            Console.WriteLine($"[build] {script.Dialogs.Count} dialogs -> {args[2]}");
        }
        else if (mode == "scan")
        {
            var outFile = args[2];
            var files = Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories)
                                 .Where(f => f.ToUpper().EndsWith(".BMD")).OrderBy(f => f).ToList();
            using var w = new StreamWriter(outFile, false, new System.Text.UTF8Encoding(false));
            int withSel = 0, scanned = 0, failed = 0;
            foreach (var f in files)
            {
                try
                {
                    var script = MessageScript.FromFile(f, FormatVersion.Detect, AtlusEncoding.Persona5RoyalCHS);
                    scanned++;
                    var sels = script.Dialogs.OfType<SelectionDialog>().ToList();
                    if (sels.Count > 0)
                    {
                        withSel++;
                        var rel = Path.GetRelativePath(src, f).Replace('\\', '/');
                        w.WriteLine($"### {rel}  dialogs={script.Dialogs.Count} selections={sels.Count}");
                        foreach (var sd in sels)
                        {
                            w.WriteLine($"  [sel pattern={sd.Pattern}] options={sd.Options.Count}");
                            for (int i = 0; i < sd.Options.Count; i++)
                                w.WriteLine($"    {i + 1}. {OptText(sd.Options[i])}");
                        }
                    }
                }
                catch { failed++; }
            }
            w.WriteLine($"\n# scanned={scanned} withSelection={withSel} failed={failed}");
            Console.WriteLine($"[scan] files={files.Count} scanned={scanned} withSelection={withSel} failed={failed}");
        }
        else if (mode == "find")
        {
            // find <root> <outfile> <kw1> <kw2> ...
            var outFile = args[2];
            var kws = args.Skip(3).ToArray();
            var files = Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories)
                                 .Where(f => f.ToUpper().EndsWith(".BMD")).OrderBy(f => f).ToList();
            using var w = new StreamWriter(outFile, false, new System.Text.UTF8Encoding(false));
            int hitFiles = 0, scanned = 0, failed = 0;
            foreach (var f in files)
            {
                try
                {
                    var script = MessageScript.FromFile(f, FormatVersion.Detect, AtlusEncoding.Persona5RoyalCHS);
                    scanned++;
                    var sw = new StringWriter();
                    using (var dec = new MessageScriptDecompiler(sw)) dec.Decompile(script);
                    var text = sw.ToString();
                    var lines = text.Split('\n');
                    var hits = new List<int>();
                    for (int i = 0; i < lines.Length; i++)
                        if (kws.Any(k => lines[i].Contains(k))) hits.Add(i);
                    if (hits.Count > 0)
                    {
                        hitFiles++;
                        w.WriteLine($"### {Path.GetRelativePath(src, f).Replace('\\','/')}  (hits={hits.Count})");
                        int last = -99;
                        foreach (var i in hits)
                        {
                            if (i - last > 1 && last >= 0) w.WriteLine("   ...");
                            w.WriteLine($"   L{i+1}: {lines[i].Trim()}");
                            last = i;
                        }
                        w.WriteLine();
                    }
                }
                catch { failed++; }
            }
            w.WriteLine($"# scanned={scanned} hitFiles={hitFiles} failed={failed}");
            Console.WriteLine($"[find] scanned={scanned} hitFiles={hitFiles} failed={failed}");
        }
    }
}
