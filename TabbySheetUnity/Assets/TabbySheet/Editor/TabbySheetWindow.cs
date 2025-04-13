using System.IO;
using TabbySheet;
using UnityEditor;
using UnityEngine;
using Logger = TabbySheet.Logger;

[InitializeOnLoad]
public class TabbySheetWindow : EditorWindow
{
    private const string EditorSettingDirectory = "Assets/TabbySheet/Editor";
    private static readonly string EditorSettingFilePath = $"{EditorSettingDirectory}/TabbySheetSettings.asset";
    private static readonly DataSheetSettings EditorDataSheetSettings = new DataSheetSettings(OnDataTableLoadHandlerEditor);
    
    private static TabbySheetSettings _dataTableSettings;
    
    static TabbySheetWindow()
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
    
    private static byte[] OnDataTableLoadHandlerEditor(string sheetName)
    {
        var asset = Resources.Load($"DataTableBinary/{sheetName}", typeof(TextAsset)) as TextAsset;
        return asset!.bytes;
    }
    
    [MenuItem("Tools/TabbySheet #t")]
    static void ShowWindow() 
    {
        LoadSettings();

        var window = GetWindow(typeof(TabbySheetWindow));
        window.titleContent = new GUIContent("TabbySheet");
        window.Show();
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    static void OnScriptsReloaded()
    {
        LoadSettings();
    }
    
    private void OnGUI()
    {
        if (_dataTableSettings == null)
        {
            LoadSettings();
            return;
        }
        
        _dataTableSettings.OnGUI();
            
        if (GUI.changed)
            SaveSettings();
    }

    private void OnDestroy()
    {
        SaveSettings();
    }

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

    private static void SaveSettings()
    {
        EditorUtility.SetDirty(DataTableSettings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
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
}
