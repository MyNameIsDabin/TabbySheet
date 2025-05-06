using System;
using TabbySheet;
using UnityEngine;

public static class TabbyDataSheet
{
    private const string DefaultAssetDirectory = "DataTableAssets";
    private static string _assetsFolder;
    
    public static void Init(string assetsFolder = null)
    {
        _assetsFolder = assetsFolder;
        DataSheet.SetDefaultSettings(new DataSheetSettings(OnDataTableLoadHandler));
    }
    
    private static byte[] OnDataTableLoadHandler(string sheetName)
    {
        var relativePath = GetPathRelativeToResources(_assetsFolder);
        var assetPath = string.IsNullOrEmpty(relativePath) ? DefaultAssetDirectory : relativePath;
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