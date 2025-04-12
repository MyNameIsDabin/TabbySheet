using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TabbySheet
{
    public interface IDataTable
    {
        public void OnLoad(IEnumerable<ISerializable> dataList);
    }
}