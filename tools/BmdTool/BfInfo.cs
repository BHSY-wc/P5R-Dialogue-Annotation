using System;
using System.Collections;
using System.IO;
using System.Reflection;
using AtlusScriptLibrary.FlowScriptLanguage.BinaryModel;

class BfInfo
{
    public static void Run(string path)
    {
        var bin = FlowScriptBinary.FromFile(path);
        var t = bin.GetType();
        Console.WriteLine($"== {Path.GetFileName(path)} size={new FileInfo(path).Length}");
        foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            object v = null;
            try { v = p.GetValue(bin); } catch (Exception ex) { Console.WriteLine($"  {p.Name}: <{ex.GetType().Name}>"); continue; }
            if (v == null) { Console.WriteLine($"  {p.Name} = null"); continue; }
            if (v is IEnumerable en && !(v is string))
            {
                Console.WriteLine($"  {p.Name}:");
                int i = 0;
                foreach (var x in en)
                {
                    if (i++ > 10) { Console.WriteLine("    ..."); break; }
                    DumpObj(x, "    ");
                }
                if (i == 0) Console.WriteLine("    (empty)");
            }
            else DumpObj(v, "  " + p.Name + " = ");
        }
    }

    static void DumpObj(object o, string prefix)
    {
        if (o == null) { Console.WriteLine(prefix + "null"); return; }
        var ty = o.GetType();
        if (ty.IsPrimitive || o is string) { Console.WriteLine(prefix + o); return; }
        var sb = new System.Text.StringBuilder();
        foreach (var f in ty.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            object v = null;
            try { v = f.GetValue(o); } catch { }
            sb.Append(f.Name).Append('=').Append(v).Append(' ');
        }
        foreach (var f in ty.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (f.GetIndexParameters().Length > 0) continue;
            object v = null;
            try { v = f.GetValue(o); } catch { }
            if (v is IEnumerable && !(v is string)) v = "<seq>";
            sb.Append(f.Name).Append('=').Append(v).Append(' ');
        }
        Console.WriteLine(prefix + ty.Name + " { " + sb + "}");
    }
}
