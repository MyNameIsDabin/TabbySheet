# Tabby Sheet

A C# and Unity-compatible library that downloads Google Sheets and exports/imports them as binary assets.

## Tabby Sheet for Unity

![](https://github.com/MyNameIsDabin/UnityGoogleSheetDataTable/blob/main/Guide/02.png)

### Unity Demo

```cs
using TabbySheet;
using UnityEngine;

public class DemoScript : MonoBehaviour
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

### How to Import 
![](https://github.com/MyNameIsDabin/TabbySheet/blob/main/Guide/07.png)

URL : `https://github.com/MyNameIsDabin/TabbySheet.git?path=/TabbySheetUnity/Assets/TabbySheet`

Click the plus (+) button in the Unity Package Manager, select 'Add package from git URL...', enter the following URL, and then click the Add button to install the package.

### Ready

You will need an OAuth client JSON file and some settings to access the Google API. This process may feel a bit cumbersome at first, but it is not a task that needs to be performed for every project.

**Once you set it up the first time, no additional configuration is required for other projects.**

First, create a project in the [Google Cloud Platform](https://cloud.google.com/free?utm_source=google&utm_medium=cpc&utm_campaign=japac-KR-all-ko-dr-BKWS-all-cloud-trial-EXA-dr-1605216&utm_content=text-ad-none-none-DEV_c-CRE_631197830139-ADGP_Hybrid%20%7C%20BKWS%20-%20EXA%20%7C%20Txt%20~%20GCP_General_google%20cloud%20platform_main-KWID_43700073965135596-kwd-528089444121&userloc_1009871-network_g&utm_term=KW_%EA%B5%AC%EA%B8%80%20%ED%81%B4%EB%9D%BC%EC%9A%B0%EB%93%9C%ED%94%8C%EB%9E%AB%ED%8F%BC&gclid=CjwKCAiAuaKfBhBtEiwAht6H73-rV5y9jwbLm-r-LY9qSXahtDxID_udznpAdZqQIgAhuJDnJv-jAxoCo88QAvD_BwE&gclsrc=aw.ds&hl=ko) and generate an OAuth client ID under the credentials section as shown below.

<img src="https://github.com/MyNameIsDabin/UnityGoogleSheetDataTable/blob/main/Guide/03.png" width="750px"></img><br/>

Download the JSON file from the information window created here. We will refer to this JSON file as 'credential.json' required by the API.

<img src="https://github.com/MyNameIsDabin/UnityGoogleSheetDataTable/blob/main/Guide/04.png" width="450px"></img><br/>


Once the Google Sheets API and Google Drive API are enabled, you are all set.

<img src="https://github.com/MyNameIsDabin/UnityGoogleSheetDataTable/blob/main/Guide/06.png" width="650px"></img><br/>

### How to use

1. Import the package file.
2. Open the sheet tool editor by selecting `[Tools - TabbySheet]` in the Unity menu or by pressing Shift+T.
3. Click the Edit button in the top left corner and enter the Google Sheet URL and the path to credential.json.
4. Click `[Download Google Sheet]` to download the sheet file from the specified URL.
5. Click `[Create Table Class]` to generate a singleton class file that allows easy access to the table data in the code.
6. Click `[Export Binary]` to convert the downloaded Excel sheet into a binary file. (You need to do this to load and access the binary data using the singleton class.)
<br>

1. package 파일을 임포트 합니다.
2. 유니티 상단의 [Tools - TabbySheet] 혹은 Shift+T 를 눌러서 시트 툴을 에디터를 열어줍니다.
3. 좌측상단의 Edit 버튼을 눌러서 Google Sheet URL, credential.json 경로를 기입해줍니다.
4. `[Download Google Sheet]`를 눌러서 해당 URL 로 부터 시트 파일을 다운로드 받습니다.
5. `[Create Table Class]`를 눌러서 코드에서 테이블 데이터에 쉽게 접근 가능하게 해주는 싱글톤 클래스 파일을 생성합니다.
6. `[Export Binary]`를 눌러서 다운로드받은 엑셀 시트를 바이너리 파일로 변환합니다. (이거까지 눌러야 싱글톤 클래스로 바이너리 데이터를 불러와서 접근 가능)

### Excel File Rules

The Excel data table you intend to use must follow the rules of the following headers.

![image](https://user-images.githubusercontent.com/26871928/218316540-ae3a2602-af48-4f31-b8a7-72a1588272e8.png)

- First line: Description of the header (any text can be used)
- Second line: Name to be used as a variable for the data 
- Third line: Supports data types **int, string, float, double, bool, and custom Enum types**. 
- Fourth line: This is optional. Currently, only UniqueKey is available, and it can be accessed in code using `GetDataBy[Name]`.
From the fifth line onwards, you can define the actual data.
<br/>

- 첫번째 라인 : 헤더의 설명 (어떤 텍스트가 들어가도 상관 없습니다)
- 두번째 라인 : 데이터의 변수로 활용될 이름 
- 세번째 라인 : 자료형 **int, string, float, double, bool, 사용자 지정 Enum 타입**을 지원합니다. 
- 네번째 라인 : 옵션입니다. 현재는 UniqueKey만 사용 가능하며, 코드상에서 `GetDataBy[이름]` 으로 접근할 수 있게 됩니다. 
- 다섯번째 라인 이후 부터는 실제 데이터가 정의되면 됩니다.

----------------------------------------
Project Copyright and License
----------------------------------------
© 2025 Davin

This software is licensed under the Apache License 2.0.
For more information, please refer to the LICENSE file.

----------------------------------------
Third-Party Dependencies
----------------------------------------

[Apache-2.0 license](https://github.com/googleapis/google-api-nodejs-client/blob/main/LICENSE)
- Google API Client Libraries (Auth, Drive.v3, Sheets.v4)

[MIT license](https://licenses.nuget.org/MIT)
- [ExcelDataReader](https://www.nuget.org/packages/exceldatareader/)
- [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/)
