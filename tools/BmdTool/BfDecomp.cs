using System;
using System.IO;
using System.Text;
using AtlusScriptLibrary.Common.Libraries;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.FlowScriptLanguage;
using AtlusScriptLibrary.FlowScriptLanguage.Decompiler;

class BfDecomp
{
    const string ModDir = @"D:/downlode/P5RMod/ReLoad2/Mods/reloaded.universal.fileemulationframework.bmd";

    public static void Run(string src, string dst)
    {
        LibraryLookup.SetLibraryPath(Path.Combine(ModDir, "Libraries"));
        LibraryLookup.EnsureInitialized();
        var lib = LibraryLookup.GetLibrary("p5r");
        Console.WriteLine("[lib] " + (lib == null ? "NULL" : lib.Name + " modules=" + lib.FlowScriptModules.Count));
        var fs = FlowScript.FromFile(src, AtlusEncoding.Persona5RoyalCHS);
        var dec = new FlowScriptDecompiler
        {
            Library = lib,
            DecompileMessageScript = false,
            StrictMode = false
        };
        bool ok = dec.TryDecompile(fs, dst);
        Console.WriteLine($"[bfdecomp] {(ok ? "ok" : "fail")} lib={lib?.Name} -> {dst}");
    }
}
