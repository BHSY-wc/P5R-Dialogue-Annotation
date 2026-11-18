using System;
using System.Linq;
using System.Reflection;

class ApiProbe
{
    public static void Run(string typeName)
    {
        var asm = typeof(AtlusScriptLibrary.FlowScriptLanguage.FlowScript).Assembly;
        foreach (var t in asm.GetTypes().Where(t => t.FullName != null && t.FullName.Contains(typeName)))
        {
            Console.WriteLine("== " + t.FullName);
            foreach (var c in t.GetConstructors())
                Console.WriteLine("  ctor(" + string.Join(", ", c.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)) + ")");
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                Console.WriteLine("  " + (m.IsStatic ? "static " : "") + m.ReturnType.Name + " " + m.Name + "(" +
                                  string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name)) + ")");
        }
    }
}
