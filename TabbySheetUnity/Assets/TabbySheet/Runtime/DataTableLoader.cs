using System;
using TabbySheet;
using UnityEngine;

public static class TabbyDataSheet
{
    public static void Init()
    {
        DataSheet.SetDefaultSettings(new DataSheetSettings(OnDataTableLoadHandler));
    }
    
    private static byte[] OnDataTableLoadHandler(string sheetName)
    {
        var assetPath = GetPathRelativeToResources(sheetName);
        var asset = Resources.Load($"{assetPath}/{sheetName}", typeof(TextAsset)) as TextAsset;
        return asset!.bytes;
    }
    
    public static string GetPathRelativeToResources(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return string.Empty;
        
        var normalized = assetPath.Replace('\\', '/');
        var parts = normalized.Split('/');
        
        for (var i = 0; i < parts.Length; i++)
        {
            if (!parts[i].Equals("Resources", StringComparison.Ordinal)) 
                continue;
            
            return i == parts.Length - 1 
                ? string.Empty 
                : string.Join("/", parts, i + 1, parts.Length - (i + 1));
        }
        return string.Empty;
    }
}