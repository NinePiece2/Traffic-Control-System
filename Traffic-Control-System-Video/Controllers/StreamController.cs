using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace Traffic_Control_System_Video.Controllers
{
    [Authorize]
    [Route("/[controller]")]
    [ApiController]
    public class StreamController : ControllerBase
    {

        public StreamController()
        {
        }

        [EnableCors]
        [HttpGet("{*filePath}")]
        public IActionResult GetFile(string filePath)
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "hls", "live");

            if (string.IsNullOrEmpty(filePath))
                return BadRequest("File path is required.");

            var normalizedFilePath = filePath.Replace('/', Path.DirectorySeparatorChar);

            var physicalFilePath = Path.Combine(basePath, normalizedFilePath);

            if (!System.IO.File.Exists(physicalFilePath))
                return NotFound();

            var contentType = GetContentType(physicalFilePath);

            var fileStream = new FileStream(physicalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(fileStream, contentType);

        }

        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".m3u8" => "application/x-mpegURL",
                ".ts" => "video/MP2T",
                _ => "application/octet-stream",
            };
        }
    }
}
