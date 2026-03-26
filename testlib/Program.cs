using System;
using Jellyfin.Data.Enums;

class P
{
    static void Main()
    {
        foreach (var p in Enum.GetNames(typeof(BaseItemKind)))
        {
            Console.WriteLine(p);
        }
    }
}
