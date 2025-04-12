using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using ExcelDataReader;
using SystemDataTable = System.Data.DataTable;

namespace TabbySheet
{
    public static class DataTableAssetGenerator
    {
        public class GenerateHandler
        {
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

        private static void InternalGenerateAsset(ExcelSheetFileMeta excelMeta, GenerateHandler generateHandler, Action<SystemDataTable> onGenerateAsset)
        {
            if (!IsExcelFile(excelMeta.FilePath))
                return;

            using var stream = File.Open(excelMeta.FilePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            
            Logger.Log("Export Binary", Logger.LogType.Debug);

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
                
                onGenerateAsset?.Invoke(table);
            }
        }
        
        public static void GenerateClassesFromExcel(ExcelSheetFileMeta excelMeta, string outputPath, GenerateHandler generateHandler = null)
        {
            InternalGenerateAsset(excelMeta, generateHandler, table =>
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
                    var funcGetObjectDataBuilder = new StringBuilder();
                    var constructorBuilder = new StringBuilder();
                    var primaryKeyBuilder = new StringBuilder();
                    var primaryDictionaryBuilder = new StringBuilder();
                    var primaryConstructorBuilder = new StringBuilder();
                    var primaryFunctionsBuilder = new StringBuilder();

                    for (var column = 0; column < table.Columns.Count; column++)
                    {
                        var fieldName = table.Rows[(int)RowTypeOfIndex.Name][column].ToString();
                        var fieldType = table.Rows[(int)RowTypeOfIndex.Type][column].ToString();
                        var fieldOption = table.Rows[(int)RowTypeOfIndex.Option][column].ToString();

                        if (string.IsNullOrEmpty(fieldName) || fieldName.StartsWith("#"))
                            continue;

                        if (Utils.TryGetTypeFromString(fieldType, out var fieldTypeName, out _))
                        {
                            fieldsBuilder.Append($"\t\t\tpublic {fieldTypeName} {fieldName} {{ get; set; }}\n");
                            funcGetObjectDataBuilder.Append($"\t\t\t\tinfo.AddValue(\"{fieldName}\", {fieldName});\n");
                            constructorBuilder.Append($"\t\t\t\t{fieldName} = ({fieldTypeName})info.GetValue(\"{fieldName}\", typeof({fieldTypeName}));\n");

                            if (Enum.TryParse<Option>(fieldOption, out var option))
                            {
                                switch (option)
                                {
                                    case Option.UniqueKey:
                                    {
                                        var fieldNameToCamelCase = Utils.ToCamelCase(fieldName);        
                                        primaryKeyBuilder.Append($"\t\t\t{fieldName},\n");

                                        var dictionaryName = $"{fieldNameToCamelCase}ToData";
                                        primaryDictionaryBuilder.Append($"\t\tprivate Dictionary<{fieldTypeName}, Data> {dictionaryName} = new Dictionary<{fieldTypeName}, Data>();\n");
                                        primaryFunctionsBuilder.Append($"\t\tpublic Data GetDataBy{fieldName}({fieldTypeName} {fieldNameToCamelCase})\n");
                                        primaryFunctionsBuilder.Append("\t\t{\n");
                                        primaryFunctionsBuilder.Append($"\t\t\treturn {dictionaryName}[{fieldNameToCamelCase}];\n");
                                        primaryFunctionsBuilder.Append("\t\t}\n\n");
                                        primaryConstructorBuilder.Append("\t\t\tforeach(var data in datas)\n");
                                        primaryConstructorBuilder.Append($"\t\t\t\t{dictionaryName}.Add(data.{fieldName}, data);\n\n");
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
                    funcGetObjectDataBuilder.Remove(funcGetObjectDataBuilder.ToString().LastIndexOf('\n'), 1);
                    constructorBuilder.Remove(constructorBuilder.ToString().LastIndexOf('\n'), 1);
                    primaryKeyBuilder.Remove(primaryKeyBuilder.ToString().LastIndexOf(",\n", StringComparison.Ordinal), 2);
                    primaryDictionaryBuilder.Remove(primaryDictionaryBuilder.ToString().LastIndexOf('\n'), 1);
                    primaryFunctionsBuilder.Remove(primaryFunctionsBuilder.ToString().LastIndexOf("\n\n", StringComparison.Ordinal), 2);
                    primaryConstructorBuilder.Remove(primaryConstructorBuilder.ToString().LastIndexOf("\n\n", StringComparison.Ordinal), 2);

                    textTemplate = textTemplate.Replace("@ClassName", className);
                    textTemplate = textTemplate.Replace("@EnumList", primaryKeyBuilder.ToString());
                    textTemplate = textTemplate.Replace("@PrimaryDictionary", primaryDictionaryBuilder.ToString());
                    textTemplate = textTemplate.Replace("@Fields", fieldsBuilder.ToString());
                    textTemplate = textTemplate.Replace("@DataConstructor", constructorBuilder.ToString());
                    textTemplate = textTemplate.Replace("@PrimaryConstructor", primaryConstructorBuilder.ToString());
                    textTemplate = textTemplate.Replace("@GetObjectData", funcGetObjectDataBuilder.ToString());
                    textTemplate = textTemplate.Replace("@PrimaryFunctions", primaryFunctionsBuilder.ToString());
                }

                classStream.Write(textTemplate);

                Logger.Log($"Create class file : {generateFilePath}");
            });
        }

        public static void GenerateBinaryFromExcel(ExcelSheetFileMeta excelMeta, string exportedPath, GenerateHandler generateHandler = null)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            InternalGenerateAsset(excelMeta, generateHandler, table =>
            {
                var exportFilePath = Path.Combine(exportedPath, $"{table.TableName}.bytes");

                using (var binaryStream = new FileStream(exportFilePath, FileMode.OpenOrCreate))
                {
                    Type tableDataType = null;
                    
                    foreach (var assembly in assemblies)
                    {
                        var type = assembly.GetType($"TabbySheet.{table.TableName}Table+Data");

                        if (type == null) 
                            continue;
                        
                        tableDataType = type;
                    }

                    if (tableDataType == null)
                        return;
                    
                    var serializer = new BinaryFormatter();

                    var dataRows = new List<ISerializable>(table.Rows.Count - (int)(RowTypeOfIndex.DataBegin - 1));

                    for (var row = (int)RowTypeOfIndex.DataBegin; row < table.Rows.Count; row++)
                    {
                        var instance = Activator.CreateInstance(tableDataType) as ISerializable;

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
                                || !Utils.TryGetTypeFromString(fieldType, out _, out var typeString))
                                continue;
                                
                            if (string.IsNullOrWhiteSpace(cell.ToString()))
                            {
                                if (cell.ToString().Length > 0)
                                    Logger.Log($"{fieldName}'s {row + 1} line data is null or whitespace. You must be delete this column.", Logger.LogType.Debug);
                                            
                                continue;
                            }
                                        
                            var converter = TypeDescriptor.GetConverter(typeString);
                            var dataValue = converter.ConvertFrom(cell.ToString());
                                        
                            Logger.Log($"{fieldName} : {dataValue}, {instance.GetType()}, {instance.GetType().GetProperty(fieldName)}", Logger.LogType.Debug);
                                        
                            var property = instance.GetType().GetProperty(fieldName);
                            property?.SetValue(instance, dataValue);
                        }

                        dataRows.Add(instance);
                    }
                    serializer.Serialize(binaryStream, dataRows);
                }
                
                var bytes = File.ReadAllBytes(exportFilePath);
                File.WriteAllBytes(exportFilePath, bytes);
            });
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