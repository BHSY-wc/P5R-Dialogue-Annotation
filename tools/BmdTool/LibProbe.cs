using System;
using System.IO;
using System.Linq;
using System.Reflection;

class LibProbe
{
    const string ModDir = @"D:/downlode/P5RMod/ReLoad2/Mods/reloaded.universal.fileemulationframework.bmd";
    public static void Run()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
        {
            var name = new AssemblyName(e.Name).Name;
            var p = Path.Combine(ModDir, name + ".dll");
            return File.Exists(p) ? Assembly.LoadFrom(p) : null;
        };
        var asm = Assembly.LoadFrom(Path.Combine(ModDir, "AtlusScriptLibrary.dll"));
        var names = new[]{"FlowScriptBinary","MessageScriptBinary","MessageScript","FlowScriptBinary+Header","BinaryFormatVersion"};
        foreach (var t in asm.GetTypes().Where(x => x.IsPublic && names.Any(n => x.Name == n || x.FullName.EndsWith(n))))
        {
            Console.WriteLine("== " + t.FullName);
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                Console.WriteLine("   prop " + p.PropertyType.Name + " " + p.Name + (p.CanWrite?" {get;set;}":" {get;}"));
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName))
                Console.WriteLine("   " + (m.IsStatic?"static ":"") + m.ReturnType.Name + " " + m.Name + "(" + string.Join(",", m.GetParameters().Select(q=>q.ParameterType.Name)) + ")");
        }
        // 找扩展方法
        foreach (var t in asm.GetTypes().Where(x => x.IsPublic && x.Name.Contains("Extension")))
            Console.WriteLine("EXT " + t.FullName + " -> " + string.Join(", ", t.GetMethods(BindingFlags.Public|BindingFlags.Static).Where(m=>!m.IsSpecialName).Select(m=>m.Name)));
    }
}
