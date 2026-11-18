using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.FlowScriptLanguage;
using AtlusScriptLibrary.FlowScriptLanguage.BinaryModel;
using AtlusScriptLibrary.FlowScriptLanguage.Compiler;
using AtlusScriptLibrary.MessageScriptLanguage;
using AtlusScriptLibrary.MessageScriptLanguage.Decompiler;

class BfMsg
{
    const string ModDir = @"D:\downlode\P5RMod\ReLoad2\Mods\reloaded.universal.fileemulationframework.bmd";
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
        AtlusScriptLibrary.Common.Libraries.LibraryLookup.SetLibraryPath(Path.Combine(ModDir, "Libraries"));
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    /// <summary>把 BF 内嵌的 MessageScript 反编译成 .msg 源码文本</summary>
    public static void Dump(string bfPath, string outMsg)
    {
        Init();
        var bin = FlowScriptBinary.FromFile(bfPath);
        var ms = MessageScript.FromBinary(bin.MessageScriptSection,
            AtlusScriptLibrary.MessageScriptLanguage.FormatVersion.Detect,
            AtlusEncoding.Persona5RoyalCHS);
        var sw = new StringWriter();
        using (var dec = new MessageScriptDecompiler(sw)) dec.Decompile(ms);
        File.WriteAllText(outMsg, sw.ToString(), new System.Text.UTF8Encoding(false));
        Console.WriteLine($"[bfmsgdump] dialogs={ms.Dialogs.Count} -> {outMsg}");
    }

    /// <summary>模拟 BF.File.Emulator 的合并流程：基 BF + .msg 导入 -> 新 BF</summary>
    public static void Merge(string baseBf, string msgFile, string outBf)
    {
        Init();
        var compiler = new FlowScriptCompiler(AtlusScriptLibrary.FlowScriptLanguage.FormatVersion.Version3BigEndian);
        compiler.Library = AtlusScriptLibrary.Common.Libraries.LibraryLookup.GetLibrary("P5R");
        compiler.Encoding = AtlusEncoding.Persona5RoyalCHS;
        compiler.ProcedureHookMode = ProcedureHookMode.ImportedOnly;
        compiler.OverwriteExistingProcedures = true;
        compiler.OverwriteExistingMsgs = true;
        compiler.AddListener(new PrintListener());

        using var fs = File.OpenRead(baseBf);
        var imports = new List<string> { msgFile };
        if (!compiler.TryCompileWithImports(fs, imports, null, out FlowScript script, out var sources))
            throw new Exception("compile failed");

        var binary = script.ToBinary();
        binary.ToFile(outBf);
        Console.WriteLine($"[bfmerge] msgs={script.MessageScript?.Dialogs.Count} procs={script.Procedures.Count} size={new FileInfo(outBf).Length}");
    }

    class PrintListener : AtlusScriptLibrary.Common.Logging.LogListener
    {
        protected override void OnLogCore(object sender, AtlusScriptLibrary.Common.Logging.LogEventArgs e)
        {
            if (e.Level >= AtlusScriptLibrary.Common.Logging.LogLevel.Warning)
                Console.WriteLine($"[compiler-{e.Level}] {e.Message}");
        }
    }
}
