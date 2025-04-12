using System;

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
        
        public static bool TryGetTypeFromString(string typeString, out string fieldName, out Type type)
        {
            fieldName = typeString.ToLower();
        
            switch (fieldName)
            {
                case "ushort":
                    type = typeof(ushort);
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