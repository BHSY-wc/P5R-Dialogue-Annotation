using System;
using System.IO;
using System.Linq;
using System.Reflection;

class BfProbe
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
        foreach (var n in new[] { "FlowScript", "FlowScriptDecompiler", "FlowScriptCompiler", "Procedure", "Instruction", "Opcode" })
        {
            foreach (var t in asm.GetTypes().Where(x => x.IsPublic && x.Name == n))
            {
                Console.WriteLine("== " + t.FullName);
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    Console.WriteLine("   static " + m.ReturnType.Name + " " + m.Name + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)) + ")");
                foreach (var c in t.GetConstructors())
                    Console.WriteLine("   ctor(" + string.Join(",", c.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)) + ")");
                foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    Console.WriteLine("   prop " + p.PropertyType.Name + " " + p.Name);
            }
        }
    }
}
