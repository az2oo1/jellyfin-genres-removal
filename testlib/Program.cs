using System;
using System.Reflection;
using System.Linq;
using MediaBrowser.Controller.Entities;

class Program
{
    static void Main()
    {
        var props = typeof(BaseItem).GetProperties().Where(p => p.Name.Contains("Studio") || p.Name.Contains("Provider")).Select(p => p.Name + " : " + p.PropertyType.Name);
        foreach(var p in props) Console.WriteLine(p);
    }
}
