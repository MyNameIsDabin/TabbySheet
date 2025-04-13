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
        var asset = Resources.Load($"DataTableBinary/{sheetName}", typeof(TextAsset)) as TextAsset;
        return asset!.bytes;
    }
}