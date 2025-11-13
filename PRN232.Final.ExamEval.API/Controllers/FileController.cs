using Microsoft.AspNetCore.Mvc;
using PRN232.Final.ExamEval.Services.IServices;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PRN232.Final.ExamEval.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        public FileController(IFileService fileService)
        {
            _fileService = fileService;
        }
        [HttpPost("upload-zip")]
        public async Task<IActionResult> UploadZip(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File zip trống");

            await _fileService.UploadZipAsync(file);
            return Ok(new { message = "Upload và lưu folder thành công" });
        }

        //[HttpGet("checkfolder")]
        //public async Task<IActionResult> CheckFolders([FromQuery] string examId)
        //{
        //    var duplicates = await _fileService.CheckDuplicateFoldersAsync(examId);
        //    if (duplicates.Any())
        //        return BadRequest(new { message = "Có thư mục trùng", duplicates });
        //    return Ok(new { message = "Không có thư mục trùng" });
        //}

        //[HttpGet("checkcode")]
        //public async Task<IActionResult> CheckCodes([FromQuery] string examId)
        //{
        //    var duplicates = await _fileService.CheckDuplicateProjectCodesAsync(examId);
        //    if (duplicates.Any())
        //        return BadRequest(new { message = "Có code trùng", duplicates });
        //    return Ok(new { message = "Không có code trùng" });
        //}
    }
}
