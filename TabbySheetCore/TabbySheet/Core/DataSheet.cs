using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TabbySheet
{
    public static class DataSheet
    {
        public delegate byte[] DataTableAssetLoadHandler(string sheetName); 
            
        private static IEnumerable<Type> _cachedTableTypes;
        private static readonly Dictionary<Type, IDataTable> _cachedTables = new();
        private static DataTableAssetLoadHandler _dataTableAssetLoadHandler;
        
        public static void SetDataTableAssetLoadHandler(DataTableAssetLoadHandler dataTableAssetLoadHandler)
            => _dataTableAssetLoadHandler = dataTableAssetLoadHandler;
        
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
            
            CheckTableAssetLoadHandlerNotNull();
            
            var dataTable = Activator.CreateInstance(tableType) as IDataTable;
            
            var loadBinaryMethodInfo = typeof(DataSheet)
                .GetMethod("LoadBinaryToList", BindingFlags.Public | BindingFlags.Static)!;

            var sheetName = tableType.Name.Replace("Table", "");
            
            var bytes = _dataTableAssetLoadHandler.Invoke(sheetName);

            var loadMethod = loadBinaryMethodInfo.MakeGenericMethod(baseType.GetGenericArguments()[1]);
                
            var result = loadMethod.Invoke(null, new object[] { bytes });
                
            dataTable?.OnLoad(((IEnumerable)result).OfType<ISerializable>());
            
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
        
        public static List<T> LoadBinaryToList<T>(byte[] binary) where T : ISerializable
        {
            using var binaryStream = new MemoryStream(binary);
            var deserializer = new BinaryFormatter();
            var data = (deserializer.Deserialize(binaryStream) as List<ISerializable>)!;
            return data.OfType<T>().ToList();
        }
        
        private static void CheckTableAssetLoadHandlerNotNull()
        {
            if (_dataTableAssetLoadHandler == null)
                throw new Exception("_dataTableAssetLoadHandler is null. Please check the SetDataTableAssetLoadHandler method.");
        }
    }
}