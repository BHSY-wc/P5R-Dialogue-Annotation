using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AtlusScriptLibrary.Common.Text.Encodings;
using AtlusScriptLibrary.MessageScriptLanguage;

/// <summary>
/// 四类标注：
///   恋人选项（带恋爱路线标志位 f7|0,1,N,1）→ 爱心
///   同一选项框里的另一项（友人路线）        → 菱形
///   其余 f8 选项（Coop 好感最优）           → 上箭头
///   f7|0,0,25,1（课堂/考试正确答案）        → 星
///
/// 恋爱标志位是 P5R 自己写在选项里的「设置剧情flag」token，值为 729~737、745，
/// 分别对应 10 位可攻略女性角色的恋爱路线；带这个位的选项就是「恋人选项」。
/// </summary>
class BmdRomance
{
    // 默认的 10 个恋爱路线标志位（新岛真/奥村春/高卷杏/佐仓双叶/千早/武见妙/川上贞代/大宅一子/东乡一二三/芳泽霞）
    public static readonly int[] DefaultLoveFlags = { 729, 730, 731, 732, 733, 734, 735, 736, 737, 745 };

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

    /// <summary>选项里若带恋爱标志位，返回该位；否则 0。</summary>
    static int LoveFlag(TokenText tt, HashSet<int> love)
    {
        foreach (var t in tt.Tokens)
        {
            if (!(t is FunctionToken ft) || ft.FunctionIndex != 7) continue;
            string a;
            try { a = string.Join(",", ft.Arguments); } catch { continue; }
            var parts = a.Split(',');
            if (parts.Length < 4) continue;
            if (parts[0] != "0" || parts[1] != "1" || parts[3] != "1") continue;
            if (int.TryParse(parts[2], out int flag) && love.Contains(flag)) return flag;
        }
        return 0;
    }

    static bool AppendMark(TokenText toks, string mark)
    {
        var list = toks.Tokens as IList<Token>;
        if (list == null) return false;

        // 找最后一个「有内容」的文本片段（末尾可能还有只含空格的片段，跳过它们）
        int i = -1;
        for (int k = list.Count - 1; k >= 0; k--)
            if (list[k] is StringToken s0 && s0.Value.Trim().Length > 0) { i = k; break; }
        if (i < 0) return false;

        var st = (StringToken)list[i];

        // 符号插在该片段末尾空白之前，紧贴正文；原有空白一个不动
        var val = st.Value;
        int end = val.Length;
        while (end > 0 && char.IsWhiteSpace(val[end - 1])) end--;
        var body = val.Substring(0, end);
        var tail = val.Substring(end);
        if (body.EndsWith(mark)) return false;
        list[i] = new StringToken(body + mark + tail);
        return true;
    }

    static (int love, int friend, int best, int answer) Tag(
        MessageScript script, HashSet<int> love, string mLove, string mFriend, string mBest, string mAnswer,
        List<string> warns)
    {
        int nl = 0, nf = 0, nb = 0, na = 0;
        foreach (var d in script.Dialogs)
        {
            if (!(d is SelectionDialog sd)) continue;
            var opts = sd.Options;
            var done = new HashSet<int>();

            // 1) 找恋人选项
            int idx = -1;
            for (int i = 0; i < opts.Count; i++)
            {
                if (LoveFlag(opts[i], love) != 0) { idx = i; break; }
            }

            if (idx >= 0)
            {
                if (AppendMark(opts[idx], mLove)) nl++;
                done.Add(idx);

                // 2) 两选一时，另一项就是友人路线
                if (opts.Count == 2)
                {
                    int other = idx == 0 ? 1 : 0;
                    if (AppendMark(opts[other], mFriend)) nf++;
                    done.Add(other);
                }
                else warns.Add($"{d.Name} 有 {opts.Count} 个选项，只标了恋人、未标友人");
            }

            // 3) 其余：好感最优 -> 上箭头，课堂答案 -> 星
            for (int i = 0; i < opts.Count; i++)
            {
                if (done.Contains(i)) continue;
                if (HasFunc(opts[i], 8, null)) { if (AppendMark(opts[i], mBest)) nb++; }
                else if (HasFunc(opts[i], 7, "0,0,25,1")) { if (AppendMark(opts[i], mAnswer)) na++; }
            }
        }
        return (nl, nf, nb, na);
    }

    public static void RunDir(string srcRoot, string dstRoot,
                              string mLove, string mFriend, string mBest, string mAnswer,
                              string flagList = null)
    {
        var love = new HashSet<int>(DefaultLoveFlags);
        if (!string.IsNullOrWhiteSpace(flagList))
        {
            love.Clear();
            foreach (var s in flagList.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                if (int.TryParse(s.Trim(), out int v)) love.Add(v);
        }

        var files = Directory.EnumerateFiles(srcRoot, "*", SearchOption.AllDirectories)
                             .Where(f => f.ToUpper().EndsWith(".BMD")).OrderBy(f => f).ToList();

        int ok = 0, skip = 0, fail = 0, cl = 0, cf = 0, cb = 0, ca = 0;
        var warns = new List<string>();

        foreach (var f in files)
        {
            var rel = Path.GetRelativePath(srcRoot, f);
            try
            {
                var script = MessageScript.FromFile(f, FormatVersion.Detect, AtlusEncoding.Persona5RoyalCHS);
                var (l, fr, b, a) = Tag(script, love, mLove, mFriend, mBest, mAnswer, warns);
                if (l + fr + b + a == 0) { skip++; continue; }
                var outp = Path.Combine(dstRoot, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(outp));
                script.ToFile(outp);
                ok++; cl += l; cf += fr; cb += b; ca += a;
            }
            catch (Exception ex) { fail++; Console.WriteLine($"[fail] {rel}: {ex.Message}"); }
        }

        Console.WriteLine($"[bmdromance] files={ok} skipped={skip} failed={fail}");
        Console.WriteLine($"  恋人{mLove}={cl}  友人{mFriend}={cf}  好感{mBest}={cb}  答案{mAnswer}={ca}");
        if (warns.Count > 0)
        {
            Console.WriteLine($"  [warn] {warns.Count} 处选项框不是两选一：");
            foreach (var w in warns.Take(20)) Console.WriteLine("    " + w);
        }
        Console.WriteLine($"  -> {dstRoot}");
    }
}
