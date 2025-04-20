using System;
using DataTables;
using TabbySheet;
using UnityEngine;

[Serializable]
public class TabbySheetSettings : ScriptableObject
{
    public string GoogleSheetURL = "https://docs.google.com/spreadsheets/d/000000~~/edit#gid=000000000";
    public string CredentialJsonPath = $"Assets/TabbySheet/Editor/credentials.json";
    public string DownloadDirectory = "Assets/TabbySheet/Editor/ExcelSheets";
    public string ExportClassFileDirectory = "Assets/TabbySheet/Tables";
    public string ExportAssetDirectory = "Assets/TabbySheet/Resources/DataTableAssets";
    public bool IsDebugMode;
    public CustomExcelSheetFileMeta DownloadedSheet;

    public class ExcelMetaAssigner : IExcelMetaAssigner<ExcelSheetInfo>
    {
        private readonly bool _isIgnore;
        
        public ExcelMetaAssigner(bool isIgnore)
        {
            _isIgnore = isIgnore;
        }
        
        public ExcelSheetInfo Assign(System.Data.DataTable dataTable)
        {
            return new ExcelSheetInfo
            {
                Name = dataTable.TableName,
                Rows = dataTable.Rows.Count,
                CustomProperties = new CustomSheetProperty
                {
                    IsIgnore = _isIgnore
                }
            };
        }
    }
}
