using System;
using System.Reflection;
using MediaBrowser.Controller.Library;

class P
{
    static void Main()
    {
        var libManagerType = typeof(ILibraryManager);
        var methods = libManagerType.GetMethods();
        foreach (var m in methods)
        {
            if (m.Name.Contains("GetItem"))
            {
                Console.WriteLine(m.Name + " returns " + m.ReturnType.Name);
            }
        }
    }
}
