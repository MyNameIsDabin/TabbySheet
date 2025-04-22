using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using TabbySheet;
using UnityEngine;

[Serializable]
public class CustomExcelSheetFileMeta : ExcelSheetFileMeta
{
    [SerializeField] 
    public DateTime DownloadTime;
    
    [SerializeField]
    public List<ExcelSheetInfo> ExcelSheetInfos = new List<ExcelSheetInfo>();
    
    public override List<ISheetInfo> SheetInfos => ExcelSheetInfos.OfType<ISheetInfo>().ToList();

    public CustomExcelSheetFileMeta()
    {
        DownloadTime = DateTime.Now;
        ObservableSheetInfos.CollectionChanged += CollectionChanged;
    }

    private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (var newItem in e.NewItems)
                    ExcelSheetInfos.Add((ExcelSheetInfo)newItem);
                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (var oldItem in e.OldItems)
                    ExcelSheetInfos.Remove((ExcelSheetInfo)oldItem);
                break;
        }
    }
}