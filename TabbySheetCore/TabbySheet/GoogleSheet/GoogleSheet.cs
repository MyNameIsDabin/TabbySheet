using System;
using System.IO;
using System.Text.RegularExpressions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace TabbySheet
{
    public class GoogleSheet 
    {
        public enum DownloadResult
        {
            Success = 0,
            NotFoundCredentialFile = 1,
            UnknownError = 2
        }
        
        private static readonly string ExcelMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        
        public static DownloadResult DownloadExcelFile(
            string appName, 
            string credentialPath, 
            string downloadPath, 
            string googleSheetUrl, 
            out string outputPath)
        {
            outputPath = null;
            
            if (File.Exists(credentialPath) == false)
            {
                Logger.Log("For accessing the Google API, a service account JSON file is required. For more details, please refer to the README.md.");
                return DownloadResult.NotFoundCredentialFile;
            }

            try
            {
                GoogleCredential credential;
                
                using (var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly, DriveService.Scope.DriveReadonly);
                }
                
                var serviceInitializer = new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = appName,
                };
                
                var sheetService = new SheetsService(serviceInitializer);
                var matches = Regex.Matches(googleSheetUrl, @"(https://docs.google.com/spreadsheets/d/)(.*)(\/.*)");
                var sheetId = matches[0].Groups[2].Value;
                var spreadSheet = sheetService.Spreadsheets.Get(sheetId).Execute();

                var driveService = new DriveService(serviceInitializer);
                var request = driveService.Files.Export(spreadSheet.SpreadsheetId, ExcelMimeType);

                using var memoryStream = new MemoryStream();
                request.Download(memoryStream);
                
                var fileName = driveService.Files.Get(spreadSheet.SpreadsheetId).Execute().Name;
                outputPath = Path.Combine(downloadPath, $"{fileName}.xlsx");
                using var file = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                memoryStream.WriteTo(file);
            }
            catch (Exception e)
            {
                Logger.Log($"Error occurred while downloading Google Sheet: {e.Message}");
                return DownloadResult.UnknownError;
            }

            return DownloadResult.Success;
        }
    }   
}
