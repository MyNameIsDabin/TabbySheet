using Godot;
using TabbySheet;

public static class TabbyDataSheet
{
	public static void Init()
	{
		DataSheet.SetDefaultSettings(new DataSheetSettings(OnDataTableLoadHandler));
	}
	
	private static byte[] OnDataTableLoadHandler(string sheetName)
	{
		var fileArray = FileAccess.GetFileAsBytes($"res://TableAssets/{sheetName}.json");
		return fileArray;
	}
}
