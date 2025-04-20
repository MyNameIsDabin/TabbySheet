using System;
using DataTables;
using TabbySheet;
using UnityEngine;

[Serializable]
public class ExcelSheetInfo : ISheetInfo<CustomSheetProperty>
{
    [SerializeField] private string name;
    [SerializeField] private int rows;
    [SerializeField] private CustomSheetProperty customSheetProperty;
    
    public string Name
    {
        get => name; 
        set => name = value;
    }

    public int Rows
    {
        get => rows; 
        set => rows = value;
    }

    public CustomSheetProperty CustomProperties
    {
        get => customSheetProperty;
        set => customSheetProperty = value;
    }
}