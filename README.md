# Tabby Sheet

<img src = "https://github.com/MyNameIsDabin/TabbySheet/blob/main/Guide/tabbysheet_guide.png" width="65%" height="65%">

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
*※ Caution: TabbySheet for Godot Engine has not been sufficiently tested yet.*

### 🔐 Quick Setup: Service Account for Google Sheets API

Before your app can access Google Sheets, follow these one-time setup steps:

---

#### 1. Create a Google Cloud Project

- Go to [Google Cloud Console](https://console.cloud.google.com/)
- Create a new project or select an existing one

#### 2. Enable APIs

- Enable **Google Sheets API**
- Enable **Google Drive API**

#### 3. Create a Service Account

- Go to **IAM & Admin > Service Accounts**
- Create a new service account (no role needed)
- Generate a **JSON key** and download it (e.g., `credentials.json`)

#### 4. Share the Spreadsheet

- Open your target spreadsheet
- Click **Share**, then add the service account’s email (e.g., `your-service@your-project.iam.gserviceaccount.com`)
- Grant at least **Viewer** access

---

### 🛠️ How to Use TabbySheet Tool (for Unity)

<img src = "https://github.com/MyNameIsDabin/TabbySheet/blob/main/Guide/unity_import_package.png" width="50%" height="50%">

URL : `https://github.com/MyNameIsDabin/TabbySheet.git?path=/TabbySheetUnity/Assets/TabbySheet`

Click the plus (+) button in the Unity Package Manager, select 'Add package from git URL...', enter the following URL, and then click the Add button to install the package.

<img src = "https://github.com/MyNameIsDabin/TabbySheet/blob/main/Guide/tabbysheet_unity.png" width="70%" height="70%">

Open the tool via **`Tools > TabbySheet (Shift+T)`** in the Unity menu bar.

In the TabbySheet window, set the following paths:
   - **GoogleSheet URL**
   - **Path to `credentials.json`** (the service account key you created earlier)

That's it! TabbySheet is now ready to fetch and export data from your Google Spreadsheet.

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
