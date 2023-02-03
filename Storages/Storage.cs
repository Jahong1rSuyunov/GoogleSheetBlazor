using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using GoogleSheetBlazor.Data;

namespace GoogleSheetBlazor.Storages
{
    public class Storage
    {
        private static string[] scopes = { DriveService.Scope.Drive };
        private static string appName = "DriveApi";
        private const string CRED_PATH = "token.json";

        private UserCredential _credential;

        public Storage()
        {

        }
        public bool ConnectDrive()
        {
            var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);

            try
            {
                _credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    Environment.UserName,
                    CancellationToken.None,
                    new FileDataStore(CRED_PATH, true)).Result;
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public UserCredential GetCredential()
        {
            var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);

            _credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    Environment.UserName,
                    CancellationToken.None,
                    new FileDataStore(CRED_PATH, true)).Result;

            return _credential;
        }
        public DriveService GetDriveService()
        {
            GetCredential();

            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = appName
            });
        }

        //public Oauth2Service GetOauth2Service()
        //{
        //    GetCredential();

        //    return new Oauth2Service(new BaseClientService.Initializer()
        //    {
        //        HttpClientInitializer = _credential,
        //        ApplicationName = appName
        //    });
        //}

        //public string GetEmailName()
        //{
        //    var oauth2Service = GetOauth2Service();

        //    return oauth2Service.Userinfo.Get().Execute().Email;

        //}

        public List<DriveFile> GetDriveFiles(string Id = "")
        {
            var result = new List<DriveFile>();

            // Create Drive API service.
            var service = GetDriveService();

            FilesResource.ListRequest listRequest = service.Files.List();

            string query;
            if (!string.IsNullOrEmpty(Id))
                query = $"(mimeType='text/csv' "
                 + "or mimeType='application/vnd.google-apps.spreadsheet' "
                 + "or mimeType='application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' "
                + "or mimeType='application/vnd.ms-excel') "
                + $"and ('{Id}' in parents and trashed = false)";
            else
                query = $"mimeType='text/csv' "
                + "or mimeType='application/vnd.ms-excel' "
                + "or mimeType='application/vnd.google-apps.spreadsheet' "
                + "or mimeType='application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' ";

            listRequest.Q = query;

            var fileList = listRequest.Execute();

            foreach (var item in fileList.Files)
            {
                var drive = new DriveFile();
                drive.Id = item.Id;
                drive.Name = item.Name;
                drive.MimeType = item.MimeType;
                drive.Kind = item.Kind;
                result.Add(drive);
            }

            return result;
        }
        public List<DriveFile> GetDriveFolderFiles(string Id = "")
        {
            var result = new List<DriveFile>();

            // Create Drive API service.
            var service = GetDriveService();

            FilesResource.ListRequest listRequest = service.Files.List();
            string query;
            if (!string.IsNullOrEmpty(Id))
                query = $"mimeType='application/vnd.google-apps.folder' and '{Id}' in parents and trashed = false";
            else
                query = "mimeType='application/vnd.google-apps.folder'";

            listRequest.Q = query;

            var fileList = listRequest.Execute();

            foreach (var item in fileList.Files)
            {
                var drive = new DriveFile();
                drive.Id = item.Id;
                drive.Name = item.Name;
                drive.MimeType = item.MimeType;
                drive.Kind = item.Kind;
                result.Add(drive);
            }

            return result;
        }
        public FileStream SheetDownload(DriveFile driveFile)
        {
            var result = new FileStream(driveFile.Name, FileMode.Create, FileAccess.Write);
            var service = GetDriveService();

            var request = service.Files.Export(driveFile.Id, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            var stream = new MemoryStream();


            request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                    case DownloadStatus.Completed:
                        {
                            Console.WriteLine("Download complete.");
                            break;
                        }
                    case DownloadStatus.Failed:
                        {
                            Console.WriteLine($"Download failed. {progress.Exception.Message}");
                            break;
                        }
                }
            };

            request.Download(stream);

            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(result);

            using (var fileStream = new FileStream("C:\\Users\\DataMicron X\\Downloads\\" + $"{Guid.NewGuid()}.xlsx", FileMode.Create, FileAccess.Write))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
            };
            stream.Close();
            return result;
        }
        public FileStream CsvDownload(DriveFile driveFile)
        {
            var result = new FileStream(driveFile.Name, FileMode.Create, FileAccess.Write);
            var service = GetDriveService();

            var request = service.Files.Get(driveFile.Id);

            var stream = new MemoryStream();

            request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                    case DownloadStatus.Completed:
                        {
                            Console.WriteLine("Download complete.");
                            break;
                        }
                    case DownloadStatus.Failed:
                        {
                            Console.WriteLine($"Download failed. {progress.Exception.Message}");
                            break;
                        }
                }
            };
            request.Download(stream);

            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(result);

            using (var fileStream = new FileStream("C:\\Users\\DataMicron X\\Downloads\\" + $"{Guid.NewGuid()}.csv", FileMode.Create, FileAccess.Write))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
            };
            stream.Close();
            return result;
        }
        public FileStream ExcelDownload(DriveFile driveFile)
        {
            var result = new FileStream(driveFile.Name, FileMode.Create, FileAccess.Write);
            var service = GetDriveService();

            var request = service.Files.Get(driveFile.Id);

            var stream = new MemoryStream();

            request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                    case DownloadStatus.Completed:
                        {
                            Console.WriteLine("Download complete.");
                            break;
                        }
                    case DownloadStatus.Failed:
                        {
                            Console.WriteLine($"Download failed. {progress.Exception.Message}");
                            break;
                        }
                }
            };
            request.Download(stream);

            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(result);

            using (var fileStream = new FileStream("C:\\Users\\DataMicron X\\Downloads\\" + $"{Guid.NewGuid()}.xlsx", FileMode.Create, FileAccess.Write))
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fileStream);
            };
            stream.Close();
            return result;
        }
    }
}
