using System;
using System.Collections.Generic;
using System.IO;
using TabbySheet;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = TabbySheet.Logger;

[InitializeOnLoad]
public class TabbySheetEditor : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    
    private const string EditorSettingDirectory = "Assets/TabbySheet/Editor";
    private static readonly string EditorSettingFilePath = $"{EditorSettingDirectory}/TabbySheetSettings.asset";
    private static readonly DataSheetSettings EditorDataSheetSettings = new(OnDataTableLoadHandlerEditor);
    
    private const string tabClassName = "tab";
    private const string currentlySelectedTabClassName = "currentlySelectedTab";
    private const string unselectedContentClassName = "unselectedContent";
    private const string tabNameSuffix = "Tab";
    private const string contentNameSuffix = "Content";
    
    private TextField googleSheetUrlField;
    private TextField credentialFilePathField;
    private TextField sheetDownloadPathField;
    private TextField classGeneratedPathField;
    private TextField assetsGeneratedPathField;
    private Toggle debugToggle;
    private Button downloadButton;
    private Button generateClassButton;
    private Button generateAssetButton;
    private Button uncheckAllButton;
    private Button checkAllButton;
    private VisualElement sheetListControls;
    private VisualElement sheetList;
    private Label lastLoadTimeLabel;
    private List<Toggle> sheetToggles = new List<Toggle>();
    
    static TabbySheetEditor()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
    }
    
    private static void OnEditorUpdate()
    {
        if (Application.isPlaying)
            return;
        
        DataSheet.SetDefaultSettings(EditorDataSheetSettings);
    }

    [MenuItem("Tools/TabbySheet #t")]
    public static void ShowTabbySheetWindow()
    {
        var wnd = GetWindow<TabbySheetEditor>();
        wnd.titleContent = new GUIContent("TabbySheet");
    }
    
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        LoadSettings();
    }
    
    private static TabbySheetSettings _dataTableSettings;
    
    private static TabbySheetSettings DataTableSettings
    {
        get
        {
            var settings = AssetDatabase.LoadAssetAtPath(EditorSettingFilePath, typeof(TabbySheetSettings)) as TabbySheetSettings;

            if (settings != null)
                return settings;
            
            if (!Directory.Exists(EditorSettingDirectory))
                Directory.CreateDirectory(EditorSettingDirectory);
            
            var asset = CreateInstance<TabbySheetSettings>();
            AssetDatabase.CreateAsset(asset, EditorSettingFilePath);
            
            return asset;
        }
    }
    
    private static void LoadSettings()
    {
        _dataTableSettings = DataTableSettings;
        
        Logger.SetLogAction((logType, message) =>
        {
            if (_dataTableSettings.IsDebugMode && logType == Logger.LogType.Debug)
                Debug.Log(message);
        });
    }
    
    public void CreateGUI()
    {
        LoadSettings();
        
        var root = rootVisualElement;

        var labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);
        
        googleSheetUrlField = root.Q<TextField>("GoogleSheetUrl");
        credentialFilePathField = root.Q<TextField>("CredentialFilePath");
        sheetDownloadPathField = root.Q<TextField>("SheetDownloadPath");
        classGeneratedPathField = root.Q<TextField>("ClassGeneratedPath");
        assetsGeneratedPathField = root.Q<TextField>("AssetsGeneratedPath");
        debugToggle = root.Q<Toggle>("DebugToggle");
        downloadButton = root.Q<Button>("DownloadButton");
        generateClassButton = root.Q<Button>("GenerateClassButton");
        generateAssetButton = root.Q<Button>("GenerateAssetButton");
        uncheckAllButton = root.Q<Button>("UncheckAllButton");
        checkAllButton = root.Q<Button>("CheckAllButton");
        sheetList = root.Q<VisualElement>("SheetList");
        sheetListControls = root.Q<VisualElement>(className:"sheet-list-controls");
        lastLoadTimeLabel = root.Q<Label>("LastLoadTime");
        
        UpdateUIFromSettings();
        UpdateLastLoadTime();
        UpdateSheetListControlsVisibility();
        
        if (_dataTableSettings.DownloadedSheet != null 
            && !string.IsNullOrEmpty(_dataTableSettings.DownloadedSheet.FilePath)
            && File.Exists(_dataTableSettings.DownloadedSheet.FilePath))
        {
            UpdateSheetList();
        }
        
        RegisterCallbacks();
        
        var tabs = GetAllTabs();
        tabs.ForEach(tab => tab.RegisterCallback<ClickEvent>(TabOnClick));
        SelectTab(tabs.First());
    }

    private void RegisterCallbacks()
    {
        googleSheetUrlField.RegisterValueChangedCallback(evt => {
            _dataTableSettings.GoogleSheetURL = evt.newValue;
            SaveSettings();
        });

        credentialFilePathField.RegisterValueChangedCallback(evt => {
            _dataTableSettings.CredentialJsonPath = evt.newValue;
            SaveSettings();
        });

        sheetDownloadPathField.RegisterValueChangedCallback(evt => {
            _dataTableSettings.DownloadDirectory = evt.newValue;
            SaveSettings();
        });

        classGeneratedPathField.RegisterValueChangedCallback(evt => {
            _dataTableSettings.ExportClassFileDirectory = evt.newValue;
            SaveSettings();
        });

        assetsGeneratedPathField.RegisterValueChangedCallback(evt => {
            _dataTableSettings.ExportAssetDirectory = evt.newValue;
            SaveSettings();
        });
        
        debugToggle.RegisterValueChangedCallback(evt => {
            _dataTableSettings.IsDebugMode = evt.newValue;
            SaveSettings();
        });

        downloadButton.clicked += OnDownloadButtonClicked;
        generateClassButton.clicked += OnGenerateClassButtonClicked;
        generateAssetButton.clicked += OnGenerateAssetButtonClicked;
        uncheckAllButton.clicked += OnUncheckAllButtonClicked;
        checkAllButton.clicked += OnCheckAllButtonClicked;
    }

    private void UpdateUIFromSettings()
    {
        googleSheetUrlField.value = _dataTableSettings.GoogleSheetURL;
        credentialFilePathField.value = _dataTableSettings.CredentialJsonPath;
        sheetDownloadPathField.value = _dataTableSettings.DownloadDirectory;
        classGeneratedPathField.value = _dataTableSettings.ExportClassFileDirectory;
        assetsGeneratedPathField.value = _dataTableSettings.ExportAssetDirectory;
    }

    private void SaveSettings()
    {
        EditorUtility.SetDirty(DataTableSettings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    private void OnDownloadButtonClicked()
    {
        CreateDirectoryIfNotExists(_dataTableSettings.DownloadDirectory);
        
        var downloadResult = GoogleSheet.DownloadExcelFile(
            Application.companyName, 
            _dataTableSettings.CredentialJsonPath, 
            _dataTableSettings.DownloadDirectory, 
            _dataTableSettings.GoogleSheetURL, out var outputPath);

        if (downloadResult != GoogleSheet.DownloadResult.Success)
        {
            Debug.LogError($"Google Sheet Download Error: {downloadResult}. Check Your Credential Json path and try again.");
            return;
        }

        _dataTableSettings.DownloadedSheet = new CustomExcelSheetFileMeta();
        var sheetInfo = _dataTableSettings.DownloadedSheet?.LoadFromFile(outputPath, new TabbySheetSettings.ExcelMetaAssigner(false));

        if (sheetInfo != null)
        {
            _dataTableSettings.DownloadedSheet = (CustomExcelSheetFileMeta)sheetInfo;
            _dataTableSettings.DownloadedSheet.DownloadTime = DateTime.Now;
                
            UpdateSheetList();
            UpdateLastLoadTime();
            Debug.Log("Download Success!");
            SaveSettings();
        }
    }
    
    private void UpdateSheetList()
    {
        sheetList.Clear();
        sheetToggles.Clear();

        if (_dataTableSettings.DownloadedSheet == null)
            return;

        foreach (var sheetInfo in _dataTableSettings.DownloadedSheet.ExcelSheetInfos)
        {
            if (sheetInfo.Name.StartsWith("#"))
                continue;

            var item = new VisualElement();
            item.AddToClassList("sheet-list-item");

            var toggle = new Toggle();
            toggle.value = !sheetInfo.CustomProperties.IsIgnore;
            toggle.RegisterValueChangedCallback(evt => {
                sheetInfo.CustomProperties.IsIgnore = !evt.newValue;
                SaveSettings();
            });

            var label = new Label(sheetInfo.Name);
            
            item.Add(toggle);
            item.Add(label);
            sheetList.Add(item);
            sheetToggles.Add(toggle);
        }
        
        UpdateSheetListControlsVisibility();
    }

    private void OnGenerateClassButtonClicked()
    {
        if (_dataTableSettings.DownloadedSheet == null)
            return;

        try
        {
            CreateDirectoryIfNotExists(_dataTableSettings.ExportClassFileDirectory);
            var generateHandler = new DataTableAssetGenerator.GenerateHandler
            {
                Predicate = sheetInfo => !((ExcelSheetInfo)sheetInfo).CustomProperties.IsIgnore,
            };
            DataTableAssetGenerator.GenerateClassesFromExcelMeta(_dataTableSettings.DownloadedSheet, _dataTableSettings.ExportClassFileDirectory, generateHandler);
            Debug.Log("Class Generation Finish!");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnGenerateAssetButtonClicked()
    {
        if (_dataTableSettings.DownloadedSheet == null)
            return;

        try
        {
            CreateDirectoryIfNotExists(_dataTableSettings.ExportAssetDirectory);
            var generateHandler = new DataTableAssetGenerator.GenerateHandler
            {
                Predicate = sheetInfo => !((ExcelSheetInfo)sheetInfo).CustomProperties.IsIgnore,
            };
            DataTableAssetGenerator.GenerateTableAssetsFromExcelMeta(_dataTableSettings.DownloadedSheet, _dataTableSettings.ExportAssetDirectory, generateHandler);
            Debug.Log("Asset Export Finish!");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnUncheckAllButtonClicked()
    {
        foreach (var toggle in sheetToggles)
        {
            toggle.value = false;
        }
    }

    private void OnCheckAllButtonClicked()
    {
        foreach (var toggle in sheetToggles)
        {
            toggle.value = true;
        }
    }

    private static void CreateDirectoryIfNotExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
    
    private void UpdateSheetListControlsVisibility()
    {
        if (sheetListControls != null)
        {
            var buttons = sheetListControls.Query<Button>(className: "list-control-button");
            
            var displayStyle = sheetList.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            
            buttons.ForEach(x =>
            { 
                x.style.display = displayStyle;
            });
        }
    }
    
    private void UpdateLastLoadTime()
    {
        if (lastLoadTimeLabel == null || _dataTableSettings.DownloadedSheet == null) 
            return;
        
        var loadTime = _dataTableSettings.DownloadedSheet.DownloadTime.ToLocalTime();
        lastLoadTimeLabel.text = $"Last loaded: {loadTime:yyyy-MM-dd HH:mm:ss}";
    }
    
    private void TabOnClick(ClickEvent evt)
    {
        var clickedTab = evt.currentTarget as Label;

        if (TabIsCurrentlySelected(clickedTab)) 
            return;
        
        GetAllTabs()
            .Where(tab => tab != clickedTab && TabIsCurrentlySelected(tab))
            .ForEach(UnselectTab);
        SelectTab(clickedTab);
    }
    
    private void SelectTab(Label tab)
    {
        tab.AddToClassList(currentlySelectedTabClassName);
        var content = FindContent(tab);
        content.RemoveFromClassList(unselectedContentClassName);
    }
    
    private void UnselectTab(Label tab)
    {
        tab.RemoveFromClassList(currentlySelectedTabClassName);
        var content = FindContent(tab);
        content.AddToClassList(unselectedContentClassName);
    }
    
    private VisualElement FindContent(Label tab)
    {
        return rootVisualElement.Q(GenerateContentName(tab));
    }
    
    private static string GenerateContentName(Label tab) =>
        tab.name.Replace(tabNameSuffix, contentNameSuffix);
    
    private static bool TabIsCurrentlySelected(Label tab)
    {
        return tab.ClassListContains(currentlySelectedTabClassName);
    }
    
    private UQueryBuilder<Label> GetAllTabs()
    {
        return rootVisualElement.Query<Label>(className: tabClassName);
    }
    
    private static byte[] OnDataTableLoadHandlerEditor(string sheetName)
    {
        var assetPath = TabbyDataSheet.GetPathRelativeToResources(_dataTableSettings.ExportAssetDirectory);
        var asset = Resources.Load($"{assetPath}/{sheetName}", typeof(TextAsset)) as TextAsset;
        return asset!.bytes;
    }
}
