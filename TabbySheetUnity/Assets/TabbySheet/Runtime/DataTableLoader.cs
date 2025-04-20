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
        var asset = Resources.Load($"DataTableAssets/{sheetName}", typeof(TextAsset)) as TextAsset;
        return asset!.bytes;
    }
}