using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Services.IServices;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace PRN232.Final.ExamEval.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubmissionController : ControllerBase
    {
        private readonly Cloudinary _cloudinary;
        private readonly ISubmissionService _submissionService;
        private readonly ISubmissionImageService _submissionImageService;

        public SubmissionController(
            Cloudinary cloudinary,
            ISubmissionService submissionService,
            ISubmissionImageService submissionImageService)
        {
            _cloudinary = cloudinary;
            _submissionService = submissionService;
            _submissionImageService = submissionImageService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadSubmission(
            IFormFile file,
            int examId,
            Guid studentId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("DOCX file is required.");

            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            string filePath = Path.Combine(tempFolder, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            // ====== EXTRACT DOCX using SharpCompress ======
            using (var archive = ArchiveFactory.Open(filePath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(tempFolder, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }

            // ====== Extract images in word/media ======
            string mediaFolder = Path.Combine(tempFolder, "word", "media");
            if (!Directory.Exists(mediaFolder))
                return BadRequest("DOCX contains no images.");

            var submission = new Submission
            {
                FilePath = file.FileName,
                SubmittedAt = DateTime.Now,
                HasViolation = false,
                ExamId = examId,
                StudentId = studentId
            };

            submission = await _submissionService.CreateSubmissionAsync(submission);

            List<string> uploadedUrls = new List<string>();

            foreach (string imgPath in Directory.GetFiles(mediaFolder))
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(imgPath),
                    Folder = "exam-submissions"
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.SecureUrl == null)
                    continue;

                string url = result.SecureUrl.ToString();
                uploadedUrls.Add(url);

                var imgEntity = new SubmissionImage
                {
                    SubmissionId = submission.SubmissionId,
                    ImagePath = url
                };

                await _submissionImageService.AddImageAsync(imgEntity);
            }

            return Ok(new
            {
                message = "Submission uploaded successfully",
                submissionId = submission.SubmissionId,
                images = uploadedUrls
            });
        }
    }
}
