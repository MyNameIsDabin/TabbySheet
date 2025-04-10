using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ExcelDataReader;

namespace TabbySheet
{
    [Serializable]
    public class ExcelSheetFileMeta : ISheetFileMeta
    {
        public string FilePath { get; private set; }
        public ObservableCollection<ISheetInfo> ObservableSheetInfos { get; } = new();
        public ExcelDataSetConfiguration ExcelDataSetConfiguration { get; private set; }
        public ISheetInfo GetSheetInfoOrNullByName(string sheetName) => SheetInfos.FirstOrDefault(x => x.Name == sheetName);
        public virtual List<ISheetInfo> SheetInfos => ObservableSheetInfos.ToList();
        
        public ExcelSheetFileMeta LoadFromFile<T>(string excelPath, IExcelMetaAssigner<T> excelMetaAssigner) where T : class, ISheetInfo 
        {
            FilePath = excelPath;
            ExcelDataSetConfiguration = CreateExcelDataSetConfiguration();

            using var stream = File.Open(excelPath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var result = reader.AsDataSet(ExcelDataSetConfiguration);

            foreach (System.Data.DataTable table in result.Tables)
                ObservableSheetInfos.Add(excelMetaAssigner.Assign(table));

            return this;
        }
        
        private static ExcelDataSetConfiguration CreateExcelDataSetConfiguration()
        {
            return new ExcelDataSetConfiguration
            {
                UseColumnDataType = true,
                FilterSheet = (_, _) => true,
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = false,
                    ReadHeaderRow = rowReader => rowReader.Read(),
                    FilterRow = _ => true,
                    FilterColumn = (_, _) => true
                }
            };
        }
    }
}