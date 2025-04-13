using System.Collections;

namespace TabbySheet
{
    public interface IDataTableAssetConverter
    {
        IEnumerable ReadAssets<T>(byte[] binary);
        public void WriteAsset(string exportFilePath, IList source);
    }
}