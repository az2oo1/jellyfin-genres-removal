using System;
using System.Reflection;
using System.Linq;
using MediaBrowser.Controller.Library;

class Program
{
    static void Main()
    {
        var methods = typeof(ILibraryManager).GetMethods().Where(m => m.Name == "GetItemById");
        foreach(var method in methods) {
            Console.WriteLine("ReturnType: " + method.ReturnType.Name);
            foreach(var p in method.GetParameters()) Console.WriteLine("  " + p.ParameterType.Name + " " + p.Name);
        }
    }
}
