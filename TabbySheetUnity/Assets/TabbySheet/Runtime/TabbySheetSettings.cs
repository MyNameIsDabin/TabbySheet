using System;
using System.IO;
using DataTables;
using TabbySheet;
using UnityEngine;
using UnityEditor;

[Serializable]
public class TabbySheetSettings : ScriptableObject
{
    public string GoogleSheetURL = "https://docs.google.com/spreadsheets/d/000000~~/edit#gid=000000000";
    public string CredentialJsonPath = $"Assets/TabbySheet/Editor/credentials.json";
    public string DownloadDirectory = "Assets/TabbySheet/Editor/ExcelSheets";
    public string ExportClassFileDirectory = "Assets/TabbySheet/Tables";
    public string ExportAssetDirectory = "Assets/TabbySheet/Resources/DataTableAssets";
    public bool IsDebugMode;
    public CustomExcelSheetFileMeta DownloadedSheet;
    
    [HideInInspector]
    public bool UseOptions = true;
    
    public enum GUIAlign
    {
        Left, 
        Middle, 
        Right
    }

    private Vector2 _scrollPosition;
    
    public class ExcelMetaAssigner : IExcelMetaAssigner<ExcelSheetInfo>
    {
        private readonly bool _isIgnore;
        
        public ExcelMetaAssigner(bool isIgnore)
        {
            _isIgnore = isIgnore;
        }
        
        public ExcelSheetInfo Assign(System.Data.DataTable dataTable)
        {
            return new ExcelSheetInfo
            {
                Name = dataTable.TableName,
                Rows = dataTable.Rows.Count,
                CustomProperties = new CustomSheetProperty
                {
                    IsIgnore = _isIgnore
                }
            };
        }
    }
    
    public void OnGUI()
    {
#if UNITY_EDITOR
        GUILayout.BeginVertical("HelpBox");
        {
            GUILayout.BeginVertical();
            
            GUILayout.Space(8);
            GUIAlignText(GUIAlign.Middle,"Google Sheet Settings", EditorStyles.boldLabel);
            GUILayout.Space(8);
            
            var useOptions = EditorGUI.BeginFoldoutHeaderGroup(new Rect(14, 14, 44, 100), UseOptions, "Edit");

            if (useOptions != UseOptions)
            {
                UseOptions = useOptions;
                GUI.FocusControl("0");
            }
            
            EditorGUI.BeginDisabledGroup(!UseOptions);

            GoogleSheetURL = EditorGUILayout.TextField("GoogleSheet URL", GoogleSheetURL);
            CredentialJsonPath = EditorGUILayout.TextField("Credential.json Path", CredentialJsonPath);
            
            EditorGUI.EndDisabledGroup();

            if (UseOptions)
            {
                DownloadDirectory = EditorGUILayout.TextField("Excel Download Directory", DownloadDirectory);
                ExportClassFileDirectory = EditorGUILayout.TextField("Created Class Directory", ExportClassFileDirectory);
                ExportAssetDirectory = EditorGUILayout.TextField("Export Assets Directory", ExportAssetDirectory);
            }
            
            GUILayout.EndVertical();
            
            EditorGUI.EndFoldoutHeaderGroup();

            if (DownloadedSheet != null && !string.IsNullOrEmpty(DownloadedSheet.FilePath))
            {
                var style = new GUIStyle(EditorStyles.helpBox)
                {
                    richText = true,
                    fontSize = 12
                };

                GUILayout.Space(3);

                var loadedTime = DownloadedSheet.DownloadTime.ToLocalTime();
                GUIAlignText(GUIAlign.Middle, $"<color=yellow>{Path.GetFileNameWithoutExtension(DownloadedSheet.FilePath)}</color> is loaded. ({loadedTime})", style);
                GUILayout.Space(3);
                
                GUILayout.BeginVertical(GUI.skin.window, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                {
                    GUILayout.BeginHorizontal(GUILayout.Width(80));
                    {
                        if (GUILayout.Button("UnCheck All", GUILayout.Height(20), GUILayout.Width(100)))
                        {
                            foreach (var sheetInfo in DownloadedSheet.ExcelSheetInfos)
                                sheetInfo.CustomProperties = new CustomSheetProperty { IsIgnore = true };
                        }

                        if (GUILayout.Button("Check All", GUILayout.Height(20), GUILayout.Width(100)))
                        {
                            foreach (var sheetInfo in DownloadedSheet.ExcelSheetInfos)
                                sheetInfo.CustomProperties = new CustomSheetProperty { IsIgnore = false };
                        }
                        
                        GUILayout.FlexibleSpace();
                        
                        var isToggle = GUILayout.Toggle(IsDebugMode, "Debug Mode", GUILayout.Width(15), GUILayout.Height(20));
                        
                        if (!IsDebugMode.Equals(isToggle))
                            IsDebugMode = isToggle;
                        
                        GUILayout.Label("Debug Mode", GUILayout.Height(18));
                    }
                    GUILayout.EndHorizontal();
                    
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
                    
                    foreach (var sheetInfo in DownloadedSheet.ExcelSheetInfos)
                    {
                        if (sheetInfo.Name.StartsWith("#"))
                            continue;

                        if (sheetInfo.CustomProperties == null) 
                            continue;

                        var sheetCustomProperty = sheetInfo.CustomProperties;
                        
                        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                        {
                            var isToggle = GUILayout.Toggle(!sheetCustomProperty.IsIgnore, "", GUILayout.Width(15));

                            if (!sheetCustomProperty.IsIgnore.Equals(!isToggle))
                                sheetCustomProperty.IsIgnore = !isToggle;
                            
                            EditorGUI.BeginDisabledGroup(sheetCustomProperty.IsIgnore);
                            GUILayout.Label(sheetInfo.Name, GUILayout.ExpandWidth(true), GUILayout.Height(15));
                            EditorGUI.EndDisabledGroup();
                        }
                        GUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }

            if (GUILayout.Button("Download Google Sheet", GUILayout.Height(30)))
            {
                CreateDirectoryIfNotExists(DownloadDirectory);
                
                var downloadResult = GoogleSheet.DownloadExcelFile(
                    Application.companyName, 
                    CredentialJsonPath, 
                    DownloadDirectory, 
                    GoogleSheetURL, out var outputPath);

                if (downloadResult != GoogleSheet.DownloadResult.Success)
                {
                    Debug.LogError($"Google Sheet Download Error: {downloadResult}. Check Your Credential Json path and try again.");
                    return;
                }

                DownloadedSheet = new CustomExcelSheetFileMeta();
                
                var sheetInfo = DownloadedSheet?.LoadFromFile(outputPath, new ExcelMetaAssigner(false));

                if (sheetInfo != null)
                {
                    DownloadedSheet = (CustomExcelSheetFileMeta)sheetInfo;
                    
                    Debug.Log("Download Success!");
                    
                    EditorUtility.SetDirty(this);
                    AssetDatabase.SaveAssets();
                }
            }

            if (DownloadedSheet != null)
            {
                EditorGUI.BeginDisabledGroup(false);

                var generateHandler = new DataTableAssetGenerator.GenerateHandler
                {
                    Predicate = sheetInfo => !((ExcelSheetInfo)sheetInfo).CustomProperties.IsIgnore,
                };
                
                if (GUILayout.Button("Generate Class Files", GUILayout.Height(30)))
                {
                    try
                    {
                        CreateDirectoryIfNotExists(ExportClassFileDirectory);
                        DataTableAssetGenerator.GenerateClassesFromExcelMeta(DownloadedSheet, ExportClassFileDirectory, generateHandler);
                        
                        Debug.Log("Class Generation Finish!");
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                
                if (GUILayout.Button("Generate Asset Files", GUILayout.Height(30)))
                {
                    try
                    {
                        CreateDirectoryIfNotExists(ExportAssetDirectory);
                        DataTableAssetGenerator.GenerateTableAssetsFromExcelMeta(DownloadedSheet, ExportAssetDirectory, generateHandler);
                        
                        Debug.Log("Asset Export Finish!");
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                
                EditorGUI.EndDisabledGroup();
            }
        }
        GUILayout.EndVertical();
        
        void GUIAlignText(GUIAlign guiAlign, string text, GUIStyle style)
        {
            GUILayout.BeginHorizontal();
            if (guiAlign == GUIAlign.Middle || guiAlign == GUIAlign.Right)
                GUILayout.FlexibleSpace();
            GUILayout.Label(text, style);
            if (guiAlign == GUIAlign.Middle || guiAlign == GUIAlign.Left)
                GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
#endif
    }
    
    private static void CreateDirectoryIfNotExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}
