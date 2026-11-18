using System;
using System.IO;
using System.Linq;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.FlowScriptLanguage;
using AtlusScriptLibrary.FlowScriptLanguage.BinaryModel;
using AtlusScriptLibrary.MessageScriptLanguage;

class BfEdit
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

    static MessageScript Load(string path, out FlowScriptBinary bin)
    {
        bin = FlowScriptBinary.FromFile(path);
        return MessageScript.FromBinary(bin.MessageScriptSection, AtlusScriptLibrary.MessageScriptLanguage.FormatVersion.Detect, AtlusEncoding.Persona5RoyalCHS);
    }

    public static void RoundTrip(string src, string dst)
    {
        Init();
        var ms = Load(src, out var bin);
        bin.MessageScriptSection = ms.ToBinary();
        bin.ToFile(dst);
        Console.WriteLine($"[roundtrip] {Path.GetFileName(src)} dialogs={ms.Dialogs.Count}");
    }

    // 消息行用 msgMark；选项按 cycle 轮流；filter 为 dialog 名前缀
    public static void StarOptions(string src, string dst, string filter, string msgMark, string[] cycle, bool doMsg)
    {
        Init();
        var ms = Load(src, out var bin);
        int changed = 0, bad = 0, msgChanged = 0;
        if (doMsg)
        {
            foreach (var d in ms.Dialogs)
            {
                if (!(d is MessageDialog md) || md.Lines == null || md.Lines.Count == 0) continue;
                if (!string.IsNullOrEmpty(filter) && !d.Name.StartsWith(filter)) continue;
                var toks = md.Lines[0].Tokens;
                int pos = toks.FindLastIndex(t => t is StringToken);
                if (pos >= 0) { var st = (StringToken)toks[pos]; toks[pos] = new StringToken(st.Value + msgMark); msgChanged++; }
            }
        }
        foreach (var d in ms.Dialogs)
        {
            if (!(d is SelectionDialog sd)) continue;
            if (!string.IsNullOrEmpty(filter) && !d.Name.StartsWith(filter)) continue;
            int idx = 0;
            foreach (var opt in sd.Options)
            {
                var toks = opt.Tokens;
                string mark = cycle[idx % cycle.Length];
                idx++;
                int pos = toks.FindLastIndex(t => t is StringToken);
                if (pos >= 0) { var st = (StringToken)toks[pos]; toks[pos] = new StringToken(st.Value + mark); changed++; }
                else bad++;
            }
        }
        bin.MessageScriptSection = ms.ToBinary();
        bin.ToFile(dst);
        Console.WriteLine($"[star] options={changed} msgLines={msgChanged} skipped={bad} -> {dst}");
    }

    static int BE32(byte[] b, int o) => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];
    static void WriteBE(byte[] b, int o, int v)
    {
        b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v;
    }

    // 库的 ToFile 不更新 P5R BF 的 header/段表，这里自己拼接并修正
    static void SaveFixed(string src, FlowScriptBinary bin, string dst)
    {
        var orig = File.ReadAllBytes(src);
        var tmp = Path.GetTempFileName();
        bin.ToFile(tmp);
        var rt = File.ReadAllBytes(tmp);
        try { File.Delete(tmp); } catch { }

        int nsec = BE32(orig, 0x10);
        int msgIdx = -1, msgOff = 0, msgAddr = 0, msgLen = 0;
        for (int i = 0; i < nsec; i++)
        {
            int o = 0x20 + i * 16;
            if (BE32(orig, o) == 3) { msgIdx = i; msgOff = o; msgAddr = BE32(orig, o + 12); msgLen = BE32(orig, o + 8); break; }
        }
        if (msgIdx < 0) { Console.WriteLine("[err] message section not found"); return; }

        int tailLen = orig.Length - (msgAddr + msgLen);
        int newMsgLen = rt.Length - msgAddr - tailLen;
        if (newMsgLen <= 0) { Console.WriteLine($"[err] newMsgLen={newMsgLen} rtLen={rt.Length} msgAddr={msgAddr} tail={tailLen}"); return; }

        var result = new byte[msgAddr + newMsgLen + tailLen];
        Array.Copy(orig, 0, result, 0, msgAddr);
        Array.Copy(rt, msgAddr, result, msgAddr, newMsgLen);
        if (tailLen > 0) Array.Copy(orig, msgAddr + msgLen, result, msgAddr + newMsgLen, tailLen);

        int delta = newMsgLen - msgLen;
        WriteBE(result, 0x04, result.Length);          // Header.FileSize
        WriteBE(result, msgOff + 8, newMsgLen);        // msg section ElementCount
        for (int i = msgIdx + 1; i < nsec; i++)
        {
            int o = 0x20 + i * 16;
            WriteBE(result, o + 12, BE32(result, o + 12) + delta);
        }
        File.WriteAllBytes(dst, result);
    }

    // 按规则给指定 sel 的指定选项加标记。规则文件每行：dialogName|optionIndex|mark
    public static void MarkSel(string src, string dst, string ruleFile)
    {
        Init();
        var ms = Load(src, out var bin);
        var rules = File.ReadAllLines(ruleFile)
                        .Select(l => l.Trim())
                        .Where(l => l.Length > 0 && !l.StartsWith("#") && l.Contains('|'))
                        .ToList();
        int ok = 0, miss = 0;
        foreach (var line in rules)
        {
            var a = line.Split('|');
            if (a.Length < 3) continue;
            var name = a[0].Trim();
            if (!int.TryParse(a[1].Trim(), out var idx)) continue;
            var mark = a[2].Trim();
            var sd = ms.Dialogs.FirstOrDefault(x => x.Name == name) as SelectionDialog;
            if (sd == null || idx < 0 || idx >= sd.Options.Count) { Console.WriteLine($"[miss] {name}#{idx}"); miss++; continue; }
            var toks = sd.Options[idx].Tokens;
            // 挂到最后一个「有内容」的文本片段上，且插在末尾空白之前，保证符号紧贴正文
            int pos = -1;
            for (int k = toks.Count - 1; k >= 0; k--)
                if (toks[k] is StringToken t && !string.IsNullOrWhiteSpace(t.Value)) { pos = k; break; }
            if (pos < 0) { Console.WriteLine($"[noText] {name}#{idx}"); miss++; continue; }
            var st = (StringToken)toks[pos];
            var val = st.Value;
            int end = val.Length;
            while (end > 0 && char.IsWhiteSpace(val[end - 1])) end--;
            if (end > 0 && val.Substring(0, end).EndsWith(mark)) continue;
            toks[pos] = new StringToken(val.Substring(0, end) + mark + val.Substring(end));
            ok++;
        }
        bin.MessageScriptSection = ms.ToBinary();
        SaveFixed(src, bin, dst);
        Console.WriteLine($"[selmark] applied={ok} miss={miss} -> {dst}");
    }
}
