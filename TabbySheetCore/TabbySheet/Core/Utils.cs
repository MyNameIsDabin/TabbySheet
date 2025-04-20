using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace TabbySheet
{
    public static class Utils
    {
        private static readonly Random random = new Random();
        
        public static int RandomRange(int minInclusive, int maxExclusive)
        {
            return random.Next(minInclusive, maxExclusive);
        }
        
        public static string ToCamelCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var words = input.Split(new[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return string.Empty;

            var camelCase = words[0].ToLowerInvariant();

            for (var i = 1; i < words.Length; i++)
            {
                if (words[i].Length <= 0) 
                    continue;
                
                var word = char.ToUpperInvariant(words[i][0]) +
                           (words[i].Length > 1 ? words[i].Substring(1).ToLowerInvariant() : "");
                camelCase += word;
            }

            return camelCase;
        }
        
        private static bool TryParseArrayTypeString(string typeString, out string elementTypeName, out int arrayLength)
        {
            elementTypeName = null;
            arrayLength = -1;

            var match = Regex.Match(typeString, @"^(?<type>[^\[\]]+)\[(?<size>\d+)\]$");
            if (!match.Success)
                return false;

            elementTypeName = match.Groups["type"].Value.Trim();
            arrayLength = int.Parse(match.Groups["size"].Value);

            return true;
        }
        
        public static bool TryGetTypeFromString(string typeString, out string fieldName, out Type type, out int? arrayLength)
        {
            arrayLength = null;
            fieldName = typeString.ToLower();

            if (TryParseArrayTypeString(typeString, out var elementTypeName, out var length))
            {
                type = null;
                arrayLength = length;
                var elementTypeNameToLower = elementTypeName.ToLower();
                fieldName = $"{elementTypeNameToLower}[]";

                switch (elementTypeNameToLower)
                {
                    case "ushort": 
                        type = typeof(ushort[]); 
                        return true;
                    case "uint": 
                        type = typeof(uint[]); 
                        return true;
                    case "bool":
                    case "boolean":
                        type = typeof(bool[]);
                        return true;
                    case "char":
                        type = typeof(char[]);
                        return true;
                    case "int":
                    case "integer":
                        type = typeof(int[]);
                        return true;
                    case "float":
                        type = typeof(float[]);
                        return true;
                    case "double":
                        type = typeof(double[]);
                        return true;
                    case "long":
                        type = typeof(long[]);
                        return true;
                    case "string":
                        type = typeof(string[]);
                        return true;
                    default:
                    {
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            var enumType = asm.GetType(elementTypeName);
                        
                            if (enumType == null)
                                continue;
                        
                            fieldName = $"{elementTypeName}[]";
                            type = enumType.MakeArrayType();
                            return true;
                        }

                        type = null;
                        return false;
                    }
                }
            }
            
            switch (fieldName)
            {
                case "ushort":
                    type =typeof(ushort);
                    return true;
                case "uint":
                    type = typeof(uint);
                    return true;
                case "bool":
                case "boolean":
                    type = typeof(bool);
                    return true;
                case "char":
                    type = typeof(char);
                    return true;
                case "int":
                case "integer":
                    type = typeof(int);
                    return true;
                case "float":
                    type = typeof(float);
                    return true;
                case "double":
                    type = typeof(double);
                    return true;
                case "long":
                    type = typeof(long);
                    return true;
                case "string":
                    type = typeof(string);
                    return true;
                default:
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var enumType = asm.GetType(typeString);
                        
                        if (enumType == null)
                            continue;
                        
                        fieldName = typeString;
                        type = enumType;
                        return true;
                    }

                    type = null;
                    return false;
                }
            }
        }
    }
}