using System.Collections;

namespace TabbySheet
{
    public interface IDataTable
    {
        public void OnLoad(IEnumerable dataList);
    }
}