using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Traffic_Control_System_Video.Services;


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
            return File(stream, "application/octet-stream", file.Name);
        }

        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(IFormFile file, string deviceID)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var fileName = Path.GetFileName(file.FileName);
            var folderPath = "ClipsFiles";
            folderPath = Path.Combine(folderPath, deviceID);

            using (var stream = file.OpenReadStream())
            {
                await _FTPFileService.UploadFileAsync(stream, fileName, folderPath);
            }

            return Ok(new { message = "File uploaded successfully" });
        }
    }
}
