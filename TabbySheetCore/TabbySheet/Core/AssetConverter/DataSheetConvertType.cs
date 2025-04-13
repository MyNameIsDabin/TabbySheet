using System;

namespace TabbySheet
{
    public enum DataTableAssetConvertType
    {
        [DataTableAssetConvert(typeof(DataTableJsonAssetConverter))]
        Json,
    }

    public class DataTableAssetConvertAttribute : Attribute
    {
        private Type SorterType { get; }

        public DataTableAssetConvertAttribute(Type sorterType)
        {
            SorterType = sorterType;
        }
    
        private static Type GetConverterType(DataTableAssetConvertType sorterType)
        {
            var type = sorterType.GetType();
            var memberInfo = type.GetMember(sorterType.ToString());

            if (memberInfo.Length > 0)
            {
                var attribute = (DataTableAssetConvertAttribute)GetCustomAttribute(memberInfo[0], typeof(DataTableAssetConvertAttribute));

                if (attribute != null)
                    return attribute.SorterType;
            }
            return null;
        }

        public static IDataTableAssetConverter CreateConverter(DataTableAssetConvertType sorterType)
        {
            var converter = (IDataTableAssetConverter)Activator.CreateInstance(GetConverterType(sorterType));
            return converter;
        }
    }
}