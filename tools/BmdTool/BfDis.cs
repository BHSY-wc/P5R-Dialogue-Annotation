using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.FlowScriptLanguage;

class BfDis
{
    public const string LibRoot = @"D:/downlode/P5RMod/ReLoad2/Mods/reloaded.universal.fileemulationframework.bmd/Libraries/Persona5Royal/Modules";
    static Dictionary<int, string> _funcs;
    static Dictionary<string, (string ret, List<string> ps)> _sig;

    public static void LoadFuncs()
    {
        if (_funcs != null) return;
        _funcs = new Dictionary<int, string>();
        _sig = new Dictionary<string, (string, List<string>)>();
        foreach (var (mod, baseIdx) in new[] { ("Common", 0x0000), ("Field", 0x1000), ("AI", 0x2000), ("Social", 0x3000), ("Facility", 0x4000), ("Net", 0x5000) })
        {
            var p = Path.Combine(LibRoot, mod, "Functions.json");
            if (!File.Exists(p)) continue;
            using var doc = JsonDocument.Parse(File.ReadAllText(p));
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                var idxStr = e.GetProperty("Index").GetString();
                int idx = Convert.ToInt32(idxStr.Substring(2), 16);
                var name = e.GetProperty("Name").GetString();
                var ps = new List<string>();
                if (e.TryGetProperty("Parameters", out var parr) && parr.ValueKind == JsonValueKind.Array)
                    foreach (var pa in parr.EnumerateArray())
                        ps.Add(pa.GetProperty("Type").GetString() + " " + pa.GetProperty("Name").GetString());
                string ret = e.TryGetProperty("ReturnType", out var rt) ? rt.GetString() : "void";
                _funcs[baseIdx + (idx & 0x0FFF)] = name;
                _sig[name] = (ret, ps);
            }
        }
    }

    public static string Name(int comm)
    {
        LoadFuncs();
        return _funcs.TryGetValue(comm, out var n) ? n : ("UNK_" + comm);
    }

    public static bool IsAddPoint(int comm)
    {
        var n = Name(comm);
        return n == "CMM_ADD_POINT" || n == "CMM_ADD_POINT_ID" || n == "CMM_ADD_POINT_ID_ST";
    }

    public static bool IsSel(int comm) => Name(comm) == "SEL";
    public static bool IsMsg(int comm) => Name(comm) == "MSG";

    static int Op(object operand)
    {
        try
        {
            if (operand == null) return 0;
            var s = operand.ToString();
            if (int.TryParse(s, out var v)) return v;
            if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f)) return (int)f;
            return 0;
        }
        catch { return 0; }
    }

    public static int OperandOf(Instruction ins) => Op(ins.Operand);

    public static void SelStat(string root, string outFile)
    {
        LoadFuncs();
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                             .Where(f => f.ToUpper().EndsWith(".BF")).OrderBy(f => f).ToList();
        int ok = 0, fail = 0, both = 0;
        using var w = new StreamWriter(outFile, false, new UTF8Encoding(false));
        w.WriteLine("sels\taddpts\tchatsel\tpath");
        foreach (var f in files)
        {
            try
            {
                var fs = FlowScript.FromFile(f, AtlusEncoding.Persona5RoyalCHS);
                ok++;
                int sels = 0, addp = 0, chatsel = 0;
                foreach (var proc in fs.Procedures)
                    foreach (var ins in proc.Instructions)
                    {
                        if (ins.Opcode != Opcode.COMM) continue;
                        int v = OperandOf(ins);
                        if (IsSel(v)) sels++;
                        else if (IsAddPoint(v)) addp++;
                        else { var n = Name(v); if (n == "CHAT_SEL_LINE" || n == "CHAT_SET_SEL") chatsel++; }
                    }
                if (sels > 0 || addp > 0)
                {
                    if (sels > 0 && addp > 0) both++;
                    w.WriteLine($"{sels}\t{addp}\t{chatsel}\t{Path.GetRelativePath(root, f).Replace('\\', '/')}");
                }
            }
            catch { fail++; }
        }
        Console.WriteLine($"[bfselstat] ok={ok} fail={fail} bothSelAndAddPoint={both} -> {outFile}");
    }

    // 统计：含选项 + 含五维加点(ADD_PC_ALL_PARAM) / 含选项 + 含好感加点
    public static void ParamStat(string root, string outFile)
    {
        LoadFuncs();
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                             .Where(f => f.ToUpper().EndsWith(".BF")).OrderBy(f => f).ToList();
        int ok = 0, fail = 0, bothAp = 0;
        using var w = new StreamWriter(outFile, false, new UTF8Encoding(false));
        w.WriteLine("sels\tallparam\taddpts\tpath");
        foreach (var f in files)
        {
            try
            {
                var fs = FlowScript.FromFile(f, AtlusEncoding.Persona5RoyalCHS);
                ok++;
                int sels = 0, ap = 0, addp = 0;
                foreach (var proc in fs.Procedures)
                    foreach (var ins in proc.Instructions)
                    {
                        if (ins.Opcode != Opcode.COMM) continue;
                        int v = OperandOf(ins);
                        var n = Name(v);
                        if (n == "SEL") sels++;
                        else if (n == "ADD_PC_ALL_PARAM" || n == "FLD_PC_PARAM_ADD_ON" || n == "FLD_PC_PARAM_ADD_DISP") ap++;
                        else if (IsAddPoint(v)) addp++;
                    }
                if (sels > 0 && ap > 0)
                {
                    bothAp++;
                    w.WriteLine($"{sels}\t{ap}\t{addp}\t{Path.GetRelativePath(root, f).Replace('\\', '/')}");
                }
            }
            catch { fail++; }
        }
        Console.WriteLine($"[bfparam] ok={ok} fail={fail} selAndAllParam={bothAp} -> {outFile}");
    }

    public static void CommStat(string root, string outFile)
    {
        LoadFuncs();
        var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                             .Where(f => f.ToUpper().EndsWith(".BF")).OrderBy(f => f).ToList();
        var hist = new Dictionary<int, int>();
        var hits = new Dictionary<int, List<string>>();
        int ok = 0, fail = 0;
        foreach (var f in files)
        {
            try
            {
                var fs = FlowScript.FromFile(f, AtlusEncoding.Persona5RoyalCHS);
                ok++;
                foreach (var proc in fs.Procedures)
                    foreach (var ins in proc.Instructions)
                    {
                        if (ins.Opcode != Opcode.COMM) continue;
                        int v = OperandOf(ins);
                        hist[v] = hist.TryGetValue(v, out var c) ? c + 1 : 1;
                        if (!hits.TryGetValue(v, out var l)) { l = new List<string>(); hits[v] = l; }
                        if (!l.Contains(f)) l.Add(f);
                    }
            }
            catch { fail++; }
        }
        using var w = new StreamWriter(outFile, false, new UTF8Encoding(false));
        w.WriteLine($"# files={files.Count} ok={ok} fail={fail} distinctComm={hist.Count}");
        foreach (var kv in hist.OrderByDescending(x => x.Value))
            w.WriteLine($"{kv.Key}\t{Name(kv.Key)}\t{kv.Value}");
        w.WriteLine("\n=== 含 ADD_POINT 的文件 ===");
        foreach (var kv in hits)
            if (IsAddPoint(kv.Key))
                foreach (var f in kv.Value)
                    w.WriteLine($"{kv.Key}\t{Name(kv.Key)}\t{f}");
        Console.WriteLine($"[bfcommstat] files={ok} fail={fail} distinct={hist.Count} -> {outFile}");
    }

    public static string Disasm(string src)
    {
        LoadFuncs();
        var fs = FlowScript.FromFile(src, AtlusEncoding.Persona5RoyalCHS);
        var sb = new StringBuilder();
        sb.AppendLine($"# {Path.GetFileName(src)} procs={fs.Procedures.Count} msgs={fs.MessageScript?.Dialogs.Count}");
        foreach (var proc in fs.Procedures)
        {
            sb.AppendLine($"PROC {proc.Name}");
            int i = 0;
            foreach (var ins in proc.Instructions)
            {
                sb.Append("  ").Append(i.ToString().PadLeft(4)).Append(": ").Append(ins.Opcode);
                if (ins.Opcode == Opcode.COMM)
                {
                    int v = OperandOf(ins);
                    sb.Append(' ').Append(v).Append("  ; ").Append(Name(v));
                }
                else
                {
                    try { if (ins.Operand != null) sb.Append(' ').Append(ins.Operand); } catch { }
                }
                sb.AppendLine();
                i++;
            }
        }
        return sb.ToString();
    }
}
