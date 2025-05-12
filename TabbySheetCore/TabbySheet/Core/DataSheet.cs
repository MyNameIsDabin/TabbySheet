using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TabbySheet
{
    public static class DataSheet
    {
        private static DataSheetSettings _defaultSettings;
        private static IEnumerable<Type> _cachedTableTypes;
        private static readonly Dictionary<Type, IDataTable> _cachedTables = new();
        public static bool IsInitialized => _defaultSettings != null;

        public static void SetDefaultSettings(DataSheetSettings defaultSettings)
            => _defaultSettings = defaultSettings;
        
        public static T Load<T>() where T : class, IDataTable
            => Load(typeof(T)) as T;
        
        public static IDataTable Load(Type type)
        {
            var baseType = type.BaseType;
                
            if (baseType == null)
                return null;
                
            var tableType = baseType.GetGenericArguments()[0];

            if (_cachedTables.TryGetValue(tableType, out var cachedDataTable))
                return cachedDataTable;
            
            CheckTableAssetDefaultSettings();
            
            var dataTable = Activator.CreateInstance(tableType) as IDataTable;
            
            var sheetName = tableType.Name.Replace("Table", "");
            
            var bytes = _defaultSettings.AssetLoadHandler.Invoke(sheetName);

            var loadBinaryMethodInfo = typeof(DataSheet)
                .GetMethod(nameof(LoadBinaryToList), BindingFlags.Public | BindingFlags.Static)!;
            
            var loadMethod = loadBinaryMethodInfo.MakeGenericMethod(baseType.GetGenericArguments()[1]);
                
            var result = loadMethod.Invoke(null, new object[] { _defaultSettings.ConvertType, bytes });
            
            dataTable?.OnLoad(((IEnumerable)result).Cast<object>());
            
            _cachedTables[tableType] = dataTable;
                
            Logger.Log($"{tableType.Name} loaded.");
            
            return _cachedTables[tableType];
        }
        
        public static List<IDataTable> LoadAll()
        {
            _cachedTables.Clear();
            
            _cachedTableTypes ??= AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IDataTable)));

            return _cachedTableTypes.Select(Load).ToList();
        }
        
        public static IEnumerable LoadBinaryToList<T>(DataTableAssetConvertType convertType, byte[] binary)
        {
            var converter = DataTableAssetConvertAttribute.CreateConverter(convertType);
            return converter.ReadAssets<T>(binary);
        }
        
        private static void CheckTableAssetDefaultSettings()
        {
            if (_defaultSettings == null)
                throw new Exception("defaultSettings is null. Please check the SetDefaultSettings method.");
        }
    }
}