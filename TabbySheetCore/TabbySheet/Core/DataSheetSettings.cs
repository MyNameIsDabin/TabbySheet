namespace TabbySheet
{
    public delegate byte[] DataTableAssetLoadHandler(string sheetName);
    
    public class DataSheetSettings
    {
        public DataTableAssetConvertType ConvertType = DataTableAssetConvertType.Json;
        public DataTableAssetLoadHandler AssetLoadHandler { get; }

        public DataSheetSettings(DataTableAssetLoadHandler assetLoadHandler)
        {
            AssetLoadHandler = assetLoadHandler;
        }
    }
}