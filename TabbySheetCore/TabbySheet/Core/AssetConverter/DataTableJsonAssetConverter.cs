using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TabbySheet
{
    public class DataTableJsonAssetConverter : IDataTableAssetConverter
    {
        public IEnumerable ReadAssets<T>(byte[] binary)
        {
            var assetStrings = System.Text.Encoding.UTF8.GetString(binary);
            return JsonConvert.DeserializeObject<List<T>>(assetStrings);
        }

        public void WriteAsset(string exportFilePath, IList dataRows)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };

            var json = JsonConvert.SerializeObject(dataRows, settings);
            File.WriteAllText(exportFilePath, json);
        }
    }
}