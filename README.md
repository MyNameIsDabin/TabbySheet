# Tabby Sheet

In Unity or Godot Engine, you can download data from Google Sheets to automatically generate class files and export them as JSON data assets.

With the standalone TabbySheetCore, it's easy to implement this functionality on other .NET platforms

### Unity Demo

```cs
using TabbySheet;
using UnityEngine;

public class UnityDemo : MonoBehaviour
{
    void Start()
    {
        TabbyDataSheet.Init();
        
        var foodsTable = DataSheet.Load<FoodsTable>();
        
        foreach (var data in foodsTable)
            Debug.Log(data.Name);
    }
}
```

### Godot Engine (.NET) Demo

```cs
using TabbySheet;
using Godot;

public partial class GodotDemo : Node
{
    public override void _Ready()
    {
        TabbyDataSheet.Init();
		
        foreach (var foodData in DataSheet.Load<FoodsTable>())
            GD.Print(foodData.Name);
    }
}
```

### How to Import
![](https://github.com/MyNameIsDabin/TabbySheet/blob/main/Guide/07.png)

URL : `https://github.com/MyNameIsDabin/TabbySheet.git?path=/TabbySheetUnity/Assets/TabbySheet`

Click the plus (+) button in the Unity Package Manager, select 'Add package from git URL...', enter the following URL, and then click the Add button to install the package.

For Godot Engine, please download this repository and open the `TabbySheetGodot` project.


### SpreadSheet Rules

The Excel data table you intend to use must follow the rules of the following headers.

![image](https://github.com/MyNameIsDabin/TabbySheet/blob/main/Guide/sheat_rules.png)

- First line: Description of the header (any text can be used)
- Second line: Name to be used as a variable for the data 
- Third line: Supports data types **int, string, float, double, bool, and custom Enum types**. 
- Fourth line: This is optional. Currently, only UniqueKey is available, and it can be accessed in code using `GetDataBy[Name]`.
From the fifth line onwards, you can define the actual data.

----------------------------------------
Project Copyright and License
----------------------------------------
© 2023 Davin

This software is licensed under the Apache License 2.0.
For more information, please refer to the LICENSE file.

#### Third-Party Dependencies

[Apache-2.0 license](https://github.com/googleapis/google-api-nodejs-client/blob/main/LICENSE)
- Google API Client Libraries (Auth, Drive.v3, Sheets.v4)

[MIT license](https://licenses.nuget.org/MIT)
- [ExcelDataReader](https://www.nuget.org/packages/exceldatareader/)
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/)
