using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

// Keep source layout identifiers in the design data, rather than the scene hierarchy.
public static class ProjectHierarchyNames
{
    [Serializable] private class NameMap { public string[] oldNames, newNames; }
    private static Dictionary<string, string> names;
    public static string Clean(string value)
    {
        if (names == null)
        {
            names = new Dictionary<string, string>();
            const string path = "Assets/Editor/HierarchyNameMap.json";
            if (File.Exists(path))
            {
                var map = JsonUtility.FromJson<NameMap>(File.ReadAllText(path));
                for (int i = 0; i < map.oldNames.Length; i++) names[map.oldNames[i]] = map.newNames[i];
            }
        }
        if (names.TryGetValue(value, out var readable)) return readable;
        value = value.Replace("РІР‚вЂќ", " ").Replace("вЂ”", " ");
        value = Regex.Replace(value, @"\(?Figma\)?", "", RegexOptions.IgnoreCase);
        value = Regex.Replace(value, @"\d+:\d+", "");
        return Regex.Replace(value, @"\s+", " ").Trim(' ', '-', '—');
    }
}
