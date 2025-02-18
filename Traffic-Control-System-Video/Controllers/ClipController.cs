using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Traffic_Control_System_Video.Services;
using System.Diagnostics;


namespace Traffic_Control_System_Video.Controllers
{
    [Authorize]
    [Route("/[controller]")]
    [ApiController]
    public class ClipController : ControllerBase
    {

        private readonly IFTPFileService _FTPFileService;
        public ClipController(IFTPFileService FTPFileService)
        {
            _FTPFileService = FTPFileService;
        }

        [HttpGet("GetFile/{*filePath}")]
        public async Task<IActionResult> GetFile(string filePath)
        {
            var fullPath = $"/ClipsFiles/{filePath}";

            var folderPath = "/ClipsFiles" + (filePath.Contains("/") ? $"/{string.Join("/", filePath.Split('/').SkipLast(1))}" : "");

            var contents = await _FTPFileService.GetFolderContentsAsync(folderPath);

            var file = contents.FirstOrDefault(x => x.FullName == fullPath);
            if (file == null)
            {
                return NotFound();
            }

            var stream = await _FTPFileService.DownloadFileAsync(file.FullName);

            string contentType = file.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ? "video/mp4" : "application/octet-stream";

            return File(stream, contentType, file.Name);
        }


        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string deviceID)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            try
            {
                var fileName = Path.GetFileName(file.FileName);
                var folderPath = Path.Combine("ClipsFiles", deviceID);

                // Ensure the folder exists
                var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), folderPath);
                Directory.CreateDirectory(directoryPath);

                // Save the original video file
                var originalFilePath = Path.Combine(directoryPath, fileName);
                using (var stream = file.OpenReadStream())
                {
                    await using (var fileStream = new FileStream(originalFilePath, FileMode.Create))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }

                // Generate HLS stream using FFmpeg
                var hlsFolderPath = Path.Combine(directoryPath, "hls");
                Directory.CreateDirectory(hlsFolderPath);  // Create a subfolder for HLS files

                var hlsPlaylistPath = Path.Combine(hlsFolderPath, "playlist.m3u8");
                var hlsCommand = $"-i \"{originalFilePath}\" -c:v libx264 -c:a aac -strict -2 -preset fast -f hls -hls_time 10 -hls_list_size 0 -hls_segment_filename \"{hlsFolderPath}/segment_%03d.ts\" \"{hlsPlaylistPath}\"";

                // Define FFmpeg path based on OS
                var ffmpegPath = "ffmpeg";  // Default to "ffmpeg" in the system PATH
                if (OperatingSystem.IsWindows())
                {
                    ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "ffmpegBins", "ffmpeg.exe");
                }

                // Start FFmpeg process to generate HLS stream
                var ffmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = hlsCommand,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                // Capture FFmpeg's output and error streams for debugging
                ffmpegProcess.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        Console.WriteLine("FFmpeg Output: " + e.Data);
                };

                ffmpegProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        Console.WriteLine("FFmpeg Error: " + e.Data);
                };

                ffmpegProcess.Start();

                // Start reading the output and error streams asynchronously
                ffmpegProcess.BeginOutputReadLine();
                ffmpegProcess.BeginErrorReadLine();

                // Wait for FFmpeg to finish
                ffmpegProcess.WaitForExit();  // Consider using WaitForExit() for debugging

                if (ffmpegProcess.ExitCode != 0)
                {
                    return BadRequest(new { message = "FFmpeg process failed", exitCode = ffmpegProcess.ExitCode });
                }

                // Upload the original video file to FTP
                using (var stream = new FileStream(originalFilePath, FileMode.Open))
                {
                    await _FTPFileService.UploadFileAsync(stream, fileName, folderPath);
                }

                // Upload the HLS files (playlist and segments) to FTP
                var hlsFiles = Directory.GetFiles(hlsFolderPath);
                foreach (var hlsFile in hlsFiles)
                {
                    var hlsFileName = Path.GetFileName(hlsFile);
                    using (var stream = new FileStream(hlsFile, FileMode.Open))
                    {
                        await _FTPFileService.UploadFileAsync(stream, hlsFileName, Path.Combine(folderPath, fileName.Replace(".mp4", "").Replace(" ", "-"), "hls"));
                    }
                }

                // Optionally clean up HLS files from the server
                Directory.Delete(hlsFolderPath, true);  // Remove the HLS folder after upload

                return Ok(new { message = "File uploaded and HLS stream generated successfully" });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                return BadRequest(new { message = "An error occurred while processing the file.", error = ex.Message });
            }
        }
    }
}
