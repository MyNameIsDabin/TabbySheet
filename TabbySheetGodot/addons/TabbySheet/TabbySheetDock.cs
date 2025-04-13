using System;
using System.Linq;
using System.Text;
using Godot;
using TabbySheet;

[Tool]
public partial class TabbySheetDock : Control
{
	[Export]
	public LineEdit lineEditGoogleSheetUrl;
	[Export]
	public LineEdit lineEditCredentialJsonPath;
	[Export]
	public LineEdit lineEditExcelDownloadPath;
	[Export]
	public LineEdit lineEditGenerateClassPath;
	[Export]
	public LineEdit lineEditBinaryDirecotryPath;
	[Export]
	public Button downloadButton;
	[Export]
	public Button generateClassesButton;
	[Export]
	public Button exportBinaryButton;
	
	private TabbySheetSettings tabbySheetSettings;
	
	private const string SettingResourcePath = "res://addons/TabbySheet/TabbySheetSettings.tres";

	public string DownloadDirectory => ProjectSettings.GlobalizePath(tabbySheetSettings.ExcelDownloadPath);
	
	public string CredentialJsonPath => ProjectSettings.GlobalizePath(tabbySheetSettings.CredentialJsonPath);

	public string ExportClassFileDirectory => ProjectSettings.GlobalizePath(tabbySheetSettings.ClassDirectoryPath);

	public string ExportTableAssetDirectory => ProjectSettings.GlobalizePath(tabbySheetSettings.TableAssetDirectoryPath);
	
	public string GoogleSheetURL => tabbySheetSettings.GoogleSheetURL;

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
	
	public ExcelSheetFileMeta DownloadedSheet;
	

	public override void _Ready()
	{	
		tabbySheetSettings = GD.Load<TabbySheetSettings>(SettingResourcePath);
		
		lineEditGoogleSheetUrl.Text = tabbySheetSettings.GoogleSheetURL;
		lineEditCredentialJsonPath.Text = tabbySheetSettings.CredentialJsonPath;
		lineEditExcelDownloadPath.Text = tabbySheetSettings.ExcelDownloadPath;
		lineEditGenerateClassPath.Text = tabbySheetSettings.ClassDirectoryPath;
		lineEditBinaryDirecotryPath.Text = tabbySheetSettings.TableAssetDirectoryPath;
		
		downloadButton.Pressed += DownloadButtonPressed;
		generateClassesButton.Pressed += GenerateClassesButtonPressed;
		exportBinaryButton.Pressed += ExportBinaryButtonPressed;
		
		lineEditGoogleSheetUrl.TextChanged += SheetUrlChanged;
		lineEditCredentialJsonPath.TextChanged += CredentialJsonPathChanged;
		lineEditExcelDownloadPath.TextChanged += ExcelDownloadPathChanged;
		lineEditGenerateClassPath.TextChanged += GenerateClassPathChanged;
		lineEditBinaryDirecotryPath.TextChanged += BinaryDirectoryPathChanged;
		
		Logger.SetLogAction((_, message) =>
		{
			GD.Print(message);
		});
	}

	public void SaveSettings()
	{
		ResourceSaver.Save(tabbySheetSettings, SettingResourcePath);
	}

	public override void _Process(double delta)
	{
	}
	
	private void DownloadButtonPressed()
	{
		CreateDirectoryIfNotExists(DownloadDirectory);
				
		var downloadResult = GoogleSheet.DownloadExcelFile(
			"GodotEditor", 
			CredentialJsonPath, 
			DownloadDirectory, 
			GoogleSheetURL, out var outputPath);

		if (downloadResult != GoogleSheet.DownloadResult.Success)
		{
			GD.PrintErr($"Google Sheet Download Error: {downloadResult}. Check Your Credential Json path and try again.");
			return;
		}
		
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		DownloadedSheet = new ExcelSheetFileMeta();
		
		var sheetInfo = DownloadedSheet.LoadFromFile(outputPath, new ExcelMetaAssigner(false));
		
		if (sheetInfo == null) 
			return;
		
		DownloadedSheet = sheetInfo;
		
		ForceSheetRefresh();
		
		GD.Print("Download Success!");
	}
	
	private void ForceSheetRefresh()
	{
		DownloadedSheet.SheetInfos.Clear();
		
		foreach (var t in DownloadedSheet.ObservableSheetInfos)
			DownloadedSheet.SheetInfos.Add(t);
	}

	private void CreateDirectoryIfNotExists(string path)
	{
		if (DirAccess.DirExistsAbsolute(path) == false)
			DirAccess.MakeDirAbsolute(path);
	}
	
	private void GenerateClassesButtonPressed()
	{
		if (DownloadedSheet == null)
		{
			GD.Print("Not yet downloaded! You need to download it first.");
			return;
		}
		
		var generateHandler = new DataTableAssetGenerator.GenerateHandler
		{
			Predicate = sheetInfo => true,
		};
		
		try
		{
			ForceSheetRefresh();
			CreateDirectoryIfNotExists(ExportClassFileDirectory);
			DataTableAssetGenerator.GenerateClassesFromExcelMeta(DownloadedSheet, ExportClassFileDirectory, generateHandler);
		
			GD.Print("Class Generation Finish!");
		}
		catch (Exception e)
		{
			GD.PrintErr(e.Message);
		}
	}
	
	private void ExportBinaryButtonPressed()
	{
		if (DownloadedSheet == null)
		{
			GD.Print("Not yet downloaded! You need to download it first.");
			return;
		}
		
		var generateHandler = new DataTableAssetGenerator.GenerateHandler
		{
			Predicate = sheetInfo => true,
		};
		
		try
		{
			ForceSheetRefresh();
			CreateDirectoryIfNotExists(ExportTableAssetDirectory);
			DataTableAssetGenerator.GenerateTableAssetsFromExcelMeta(DownloadedSheet, ExportTableAssetDirectory, generateHandler);
			
			GD.Print("Data Export Finish!");
		}
		catch (Exception e)
		{
			GD.PrintErr(e.Message);
		}
	}
	
	private void SheetUrlChanged(string input)
	{
		tabbySheetSettings.GoogleSheetURL = input;
		SaveSettings();
	}
	
	private void CredentialJsonPathChanged(string input)
	{
		tabbySheetSettings.CredentialJsonPath = input;
		SaveSettings();
	}
	
	private void ExcelDownloadPathChanged(string input)
	{
		tabbySheetSettings.ExcelDownloadPath = input;
		SaveSettings();
	}
	
	private void GenerateClassPathChanged(string input)
	{
		tabbySheetSettings.ClassDirectoryPath = input;
		SaveSettings();
	}
	
	private void BinaryDirectoryPathChanged(string input)
	{
		tabbySheetSettings.TableAssetDirectoryPath = input;
		SaveSettings();
	}
}
