using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ExcelDataReader;
using SystemDataTable = System.Data.DataTable;

namespace TabbySheet
{
    public static class DataTableAssetGenerator
    {
        public class GenerateHandler
        {
            public DataTableAssetConvertType ConvertType { get; set; }
            public Func<ISheetInfo, bool> Predicate { get; set; }
        }

        private enum RowTypeOfIndex
        {
            Header,
            Name,
            Type,
            Option,
            DataBegin
        }

        private enum Option
        {
            UniqueKey
        }
        
        private const string ClassTemplateResourcePath = "TabbySheet.ExcelSheet.DataTableClassTemplate.txt";
        private static readonly string[] SupportedExtensions = { ".xls", ".xlsx" };
        private static readonly string ClassFileTemplate;

        static DataTableAssetGenerator()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(ClassTemplateResourcePath);
            
            if (stream == null)
                throw new FileNotFoundException($"{ClassTemplateResourcePath} is not found.", ClassTemplateResourcePath);

            using var reader = new StreamReader(stream);
            ClassFileTemplate = reader.ReadToEnd();
        }

        private static List<SystemDataTable> InternalGenerateAsset(ExcelSheetFileMeta excelMeta, GenerateHandler generateHandler)
        {
            var dataTables = new List<SystemDataTable>();
            
            if (!IsExcelFile(excelMeta.FilePath))
                return dataTables;

            using var stream = File.Open(excelMeta.FilePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            
            Logger.Log("Export table assets.", Logger.LogType.Debug);

            var result = reader.AsDataSet(excelMeta.ExcelDataSetConfiguration);

            foreach (SystemDataTable table in result.Tables)
            {
                var sheetInfo = excelMeta.GetSheetInfoOrNullByName(table.TableName);

                if (sheetInfo == null)
                {
                    Logger.Log($"{table.TableName} not found.", Logger.LogType.Debug);
                    continue;
                }

                if (generateHandler?.Predicate != null && !generateHandler.Predicate(sheetInfo))
                {
                    Logger.Log($"{table.TableName} is Ignored", Logger.LogType.Debug);
                    continue;
                }

                Logger.Log($"{table.TableName} (length : {table.Rows.Count})", Logger.LogType.Debug);

                if (sheetInfo.Name.StartsWith("#"))
                {
                    Logger.Log($"{table.TableName} is Ignored. (table name is started with '#')", Logger.LogType.Debug);
                    continue;
                }
                
                dataTables.Add(table);
            }

            return dataTables;
        }
        
        public static void GenerateClassesFromExcelMeta(ExcelSheetFileMeta excelMeta, string outputPath, GenerateHandler generateHandler = null)
        {
            var tables = InternalGenerateAsset(excelMeta, generateHandler);
            
            foreach (var table in tables)
            {
                var className = $"{table.TableName}Table";
                var generateFilePath = Path.Combine(outputPath, $"{className}.cs");
                
                using var classStream = new StreamWriter(new FileStream(generateFilePath, FileMode.Create));
                
                var textTemplate = ClassFileTemplate;

                for (var row = 0; row < table.Rows.Count; row++)
                {
                    if ((RowTypeOfIndex)row != RowTypeOfIndex.Name) 
                        continue;
                    
                    var fieldsBuilder = new StringBuilder();
                    var primaryKeyBuilder = new StringBuilder();
                    var uniqueDictionaryBuilder = new StringBuilder();
                    var uniqueLoadFunctionBuilder = new StringBuilder();
                    var uniqueFunctionsBuilder = new StringBuilder();

                    for (var column = 0; column < table.Columns.Count; column++)
                    {
                        var fieldName = table.Rows[(int)RowTypeOfIndex.Name][column].ToString();
                        var fieldType = table.Rows[(int)RowTypeOfIndex.Type][column].ToString();
                        var fieldOption = table.Rows[(int)RowTypeOfIndex.Option][column].ToString();

                        if (string.IsNullOrEmpty(fieldName) || fieldName.StartsWith("#"))
                            continue;

                        if (Utils.TryGetTypeFromString(fieldType, out var fieldTypeName, out _, out _))
                        {
                            fieldsBuilder.Append($"\t\t\tpublic {fieldTypeName} {fieldName} {{ get; set; }}\n");

                            if (Enum.TryParse<Option>(fieldOption, out var option))
                            {
                                switch (option)
                                {
                                    case Option.UniqueKey:
                                    {
                                        var fieldNameToCamelCase = Utils.ToCamelCase(fieldName);        
                                        primaryKeyBuilder.Append($"\t\t\t{fieldName},\n");

                                        var dictionaryName = $"{fieldNameToCamelCase}ToData";
                                        uniqueDictionaryBuilder.Append($"\t\tprivate Dictionary<{fieldTypeName}, Data> {dictionaryName} = new Dictionary<{fieldTypeName}, Data>();\n");
                                        uniqueFunctionsBuilder.Append($"\t\tpublic Data GetDataBy{fieldName}({fieldTypeName} {fieldNameToCamelCase})\n");
                                        uniqueFunctionsBuilder.Append("\t\t{\n");
                                        uniqueFunctionsBuilder.Append($"\t\t\treturn {dictionaryName}[{fieldNameToCamelCase}];\n");
                                        uniqueFunctionsBuilder.Append("\t\t}\n\n");


                                        uniqueLoadFunctionBuilder.Append("\t\tprotected override void OnSetupUniqueKey()\n");
                                        uniqueLoadFunctionBuilder.Append("\t\t{\n");
                                        uniqueLoadFunctionBuilder.Append("\t\t\tforeach(var data in datas)\n");
                                        uniqueLoadFunctionBuilder.Append($"\t\t\t\t{dictionaryName}.Add(data.{fieldName}, data);\n\n");
                                        uniqueLoadFunctionBuilder.Append("\t\t}\n\n");
                                    }
                                        break;
                                    default:
                                    {
                                        Logger.Log($"{option} option is not supported.");
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Logger.Log($"This is not supported type : {fieldType}");
                        }
                    }

                    fieldsBuilder.Remove(fieldsBuilder.ToString().LastIndexOf('\n'), 1);
                    primaryKeyBuilder.Remove(primaryKeyBuilder.ToString().LastIndexOf(",\n", StringComparison.Ordinal), 2);
                    uniqueDictionaryBuilder.Remove(uniqueDictionaryBuilder.ToString().LastIndexOf('\n'), 1);
                    uniqueFunctionsBuilder.Remove(uniqueFunctionsBuilder.ToString().LastIndexOf("\n\n", StringComparison.Ordinal), 2);
                    uniqueLoadFunctionBuilder.Remove(uniqueLoadFunctionBuilder.ToString().LastIndexOf("\n\n", StringComparison.Ordinal), 2);

                    textTemplate = textTemplate.Replace("@ClassName", className);
                    textTemplate = textTemplate.Replace("@EnumList", primaryKeyBuilder.ToString());
                    textTemplate = textTemplate.Replace("@UniqueDictionary", uniqueDictionaryBuilder.ToString());
                    textTemplate = textTemplate.Replace("@Fields", fieldsBuilder.ToString());
                    textTemplate = textTemplate.Replace("@UniqueLoadFunction", uniqueLoadFunctionBuilder.ToString());
                    textTemplate = textTemplate.Replace("@UniqueFunctions", uniqueFunctionsBuilder.ToString());
                }

                classStream.Write(textTemplate);

                Logger.Log($"Create class file : {generateFilePath}");
            }
        }
        
        public static void GenerateTableAssetsFromExcelMeta(ExcelSheetFileMeta excelMeta, string exportedPath, GenerateHandler generateHandler = null)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var tables = InternalGenerateAsset(excelMeta, generateHandler);
            
            foreach (var table in tables)
            {
                var exportFilePath = Path.Combine(exportedPath, $"{table.TableName}.json");
                
                Type tableDataType = null;
                    
                foreach (var assembly in assemblies)
                {
                    var type = assembly.GetType($"TabbySheet.{table.TableName}Table+Data");

                    if (type == null) 
                        continue;
                        
                    tableDataType = type;
                }

                if (tableDataType == null)
                {
                    Logger.Log($"{table.TableName} Class file not found.", Logger.LogType.Debug);
                    continue;
                }
                    
                var dataRows = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(tableDataType));
                    
                for (var row = (int)RowTypeOfIndex.DataBegin; row < table.Rows.Count; row++)
                {
                    var instance = Activator.CreateInstance(tableDataType);

                    if (instance == null)
                        continue;

                    for (var column = 0; column < table.Columns.Count; column++)
                    {
                        var cell = table.Rows[row][column];
                        var rowType = (RowTypeOfIndex)row;

                        var fieldName = table.Rows[(int)RowTypeOfIndex.Name][column].ToString();
                        var fieldType = table.Rows[(int)RowTypeOfIndex.Type][column].ToString();
                                
                        if (string.IsNullOrEmpty(fieldName) || fieldName.StartsWith("#"))
                            continue;
                                
                        if (rowType <= RowTypeOfIndex.Type 
                            || !Utils.TryGetTypeFromString(fieldType, out _, out var typeString, out var arrayLength))
                            continue;
                        
                        if (string.IsNullOrWhiteSpace(cell.ToString()))
                        {
                            if (cell.ToString().Length > 0)
                                Logger.Log($"{fieldName}'s {row + 1} line data is null or whitespace. You must be delete this column.", Logger.LogType.Debug);
                            continue;
                        }

                        if (typeString.IsArray && arrayLength.HasValue)
                        {
                            var elementType = typeString.GetElementType()!;
                            var converter = TypeDescriptor.GetConverter(elementType);
                            var typedArray = Array.CreateInstance(elementType, arrayLength.Value);
                            
                            for (var i = 0; i < arrayLength.Value; i++)
                            {
                                var arrayCell = table.Rows[row][column + i];

                                if (Convert.IsDBNull(arrayCell))
                                {
                                    if (elementType.IsValueType)
                                    {
                                        var defaultValue = Activator.CreateInstance(elementType);
                                        typedArray.SetValue(defaultValue, i);
                                    }
                                    
                                    continue;
                                }
                                
                                var elementValue = converter.ConvertFrom(arrayCell.ToString());
                                typedArray.SetValue(elementValue, i);
                            }
                            
                            Logger.Log($"{fieldName} : {typedArray}, {instance.GetType()}, {instance.GetType().GetProperty(fieldName)}", Logger.LogType.Debug);
                            
                            var property = instance.GetType().GetProperty(fieldName);
                            property?.SetValue(instance, typedArray);
                            
                            column += (arrayLength.Value - 1);
                        }
                        else
                        {
                            var converter = TypeDescriptor.GetConverter(typeString);
                            var dataValue = converter.ConvertFrom(cell.ToString());
                            
                            Logger.Log($"{fieldName} : {dataValue}, {instance.GetType()}, {instance.GetType().GetProperty(fieldName)}", Logger.LogType.Debug);
                            
                            var property = instance.GetType().GetProperty(fieldName);
                            property?.SetValue(instance, dataValue);   
                        }
                    } 

                    dataRows.Add(instance);
                }

                var dataTableAssetConverter = DataTableAssetConvertAttribute.CreateConverter(generateHandler?.ConvertType ?? DataTableAssetConvertType.Json);
                
                dataTableAssetConverter.WriteAsset(exportFilePath, dataRows);
            };
        }
        
        private static bool IsExcelFile(string filePath)
        {
            var fileExtension = Path.GetExtension(filePath).ToLower();

            if (SupportedExtensions.Any(ext => ext == fileExtension)) 
                return true;
            
            Logger.Log($"Only Support to ({string.Join(", ", SupportedExtensions)}) file.", Logger.LogType.Debug);
            return false;
        }
    }
}