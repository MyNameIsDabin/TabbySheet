using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TabbySheet
{
    public abstract class DataSheetBase<TTable, TData> : IEnumerable<TData>, IDataTable 
        where TData : class
        where TTable : new()
    {
        public string ColumnName;

        public Type DataType;

        protected List<TData> datas = new();

        public TData this[int index] => datas[index];

        public int Count => datas.Count;

        public IEnumerator<TData> GetEnumerator() => datas.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => datas.GetEnumerator();

        public TData GetRandom()
        {
            return datas[Utils.RandomRange(0, datas.Count - 1)];
        }

        public virtual void OnLoad(IEnumerable<object> dataList)
        {
            datas = dataList.OfType<TData>().ToList();
        }
    }
}