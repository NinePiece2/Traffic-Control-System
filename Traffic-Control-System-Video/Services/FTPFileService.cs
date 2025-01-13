using FluentFTP;
using FluentFTP.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Security.Authentication;
using Traffic_Control_System.Data;

namespace Traffic_Control_System_Video.Services
{
    public interface IFTPFileService
    {
        Task<List<FtpListItem>> GetFolderContentsAsync(string folderPath);
        Task UploadFileAsync(Stream fileStream, string fileName, string folderPath);
        Task<Stream> DownloadFileAsync(string remoteFilePath);
    }

    public class FTPFileService : IFTPFileService
    {
        private string _ftpHost = "ftp://localhost";
        private string _ftpUser = "ftpuser";
        private string _ftpPass = "ftppassword";
        private readonly ApplicationDbContext _context;
        private AsyncFtpClient client;

        public FTPFileService(ApplicationDbContext context)
        {
            _context = context;

            _ftpHost = _context.PowerSettings.Find("FTPHost")?.Value ?? _ftpHost;
            _ftpUser = _context.PowerSettings.Find("FTPUser")?.Value ?? _ftpUser;
            _ftpPass = _context.PowerSettings.Find("FTPPass")?.Value ?? _ftpPass;

            client = new AsyncFtpClient(_ftpHost, _ftpUser, _ftpPass);

            // Enable FTPS(FTP over SSL / TLS)
            client.Config.EncryptionMode = FtpEncryptionMode.Implicit; // Enable TLS
            client.Config.ValidateAnyCertificate = true;
            client.Config.DataConnectionType = FtpDataConnectionType.PASV;

            client.Config.DataConnectionEncryption = false;
            client.Config.SslProtocols = SslProtocols.Tls12;
            client.Config.LogToConsole = true;
            client.Config.InternetProtocolVersions = FtpIpVersion.IPv4;
            client.Config.CheckCapabilities = true;
            client.Config.LogHost = true;
            client.Config.LogUserName = true;
            client.Config.LogDurations = true;
            client.Config.LogPassword = true;
        }

        public async Task<List<FtpListItem>> GetFolderContentsAsync(string folderPath)
        {
            try
            {
                await client.AutoConnect();
                var items = await client.GetListing(folderPath);
                return new List<FtpListItem>(items);
            }
            catch (FtpCommandException ex)
            {
                // Log detailed error message
                Console.WriteLine($"FTP command failed: {ex.Message}");
                throw; // Re-throw or handle as needed
            }
            finally
            {
                await client.Disconnect();
            }

        }

        public async Task UploadFileAsync(Stream fileStream, string fileName, string folderPath)
        {
            await client.AutoConnect();

            // Create folder if it doesn't exist
            if (!await client.DirectoryExists(folderPath))
            {
                await client.CreateDirectory(folderPath);
            }

            // Save stream to a temporary file
            var tempFilePath = Path.GetTempFileName();
            try
            {
                using (var file = File.Create(tempFilePath))
                {
                    fileStream.Seek(0, SeekOrigin.Begin); // Make sure we're at the beginning of the stream
                    await fileStream.CopyToAsync(file);
                }

                // Upload the temp file
                await client.UploadFile(tempFilePath, Path.Combine(folderPath, fileName));
            }
            finally
            {
                // Clean up the temp file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
                await client.Disconnect();
            }
        }

        public async Task<Stream> DownloadFileAsync(string remoteFilePath)
        {
            await client.AutoConnect();

            var memoryStream = new MemoryStream();
            await client.DownloadStream(memoryStream, remoteFilePath);
            memoryStream.Position = 0; // Reset the stream position to the beginning

            await client.Disconnect();
            return memoryStream;
        }
    }
}
