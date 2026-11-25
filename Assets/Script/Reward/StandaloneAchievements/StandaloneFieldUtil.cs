using System.Reflection;
using UnityEngine;
public static class StandaloneFieldUtil
{
    private static object Read(object obj, string name)
    {
        if (obj == null) return null;
        var fi = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return fi == null ? null : fi.GetValue(obj);
    }
    public static bool ReadBool(object obj, string name) { return Read(obj, name) is bool b && b; }
    public static char ReadChar(object obj, string name) { return Read(obj, name) is char c ? c : '\0'; }
    public static GameObject ReadGameObject(object obj, string name) { return Read(obj, name) as GameObject; }
}
