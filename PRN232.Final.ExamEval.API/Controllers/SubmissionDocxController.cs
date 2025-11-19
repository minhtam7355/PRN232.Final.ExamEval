using Aspose.Words;
using Aspose.Words.Saving;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Text.Json.Serialization;

namespace PRN232.Final.ExamEval.API.Controllers
{
    [Route("api/submission-docx")]
    [ApiController]
    public class SubmissionDocxController : ControllerBase
    {
        private readonly Cloudinary _cloudinary;
        private readonly SemaphoreSlim _semaphore;
        private const int MaxConcurrentTasks = 5;

        public SubmissionDocxController(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
            _semaphore = new SemaphoreSlim(MaxConcurrentTasks);
            AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);
        }

        // ======================================================
        // POST /upload
        // ======================================================
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocxSubmission(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("ZIP file required.");

            if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only ZIP file allowed.");

            var files = ExtractZipFiles(file);

            if (files.Count == 0)
                return BadRequest("No files found in ZIP.");

            var tasks = files.Select(f => ProcessFileSafe(f.Name, f.Extension, f.Content));
            var results = await Task.WhenAll(tasks);

            return Ok(new
            {
                Total = results.Length,
                Successful = results.Count(r => r.Success),
                Failed = results.Count(r => !r.Success),
                Files = results
            });
        }

        // ======================================================
        // ZIP EXTRACTION
        // ======================================================
        private List<(string Name, string Extension, byte[] Content)> ExtractZipFiles(IFormFile file)
        {
            var allFiles = new List<(string, string, byte[])>();

            using var zip = new ZipArchive(file.OpenReadStream());

            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name) ||
                    entry.Name.StartsWith(".") ||
                    entry.Name.StartsWith("__MACOSX"))
                    continue;

                using var ms = new MemoryStream();
                entry.Open().CopyTo(ms);

                allFiles.Add((entry.Name, Path.GetExtension(entry.Name).ToLower(), ms.ToArray()));
            }

            return allFiles;
        }

        // ======================================================
        // PROCESS FILE - SAFE WRAPPER
        // ======================================================
        private async Task<ProcessResult> ProcessFileSafe(string fileName, string extension, byte[] bytes)
        {
            string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var validate = ValidateFileName(nameWithoutExt);

            if (!validate.IsValid)
            {
                return new ProcessResult
                {
                    Success = false,
                    FileName = fileName,
                    FileExtension = extension,
                    ErrorMessage = validate.Error,
                    ErrorType = "InvalidFileName"
                };
            }

            if (extension != ".docx")
            {
                return new ProcessResult
                {
                    Success = false,
                    FileName = fileName,
                    FileExtension = extension,
                    ErrorMessage = $"Invalid file format: {extension}. Only .docx files are processed.",
                    ErrorType = "InvalidFileFormat"
                };
            }

            await _semaphore.WaitAsync();
            try
            {
                using var stream = new MemoryStream(bytes);
                var result = await ProcessDocx(fileName, stream);

                // SUCCESS RETURN
                return new ProcessResult
                {
                    Success = true,
                    FileName = fileName,
                    FileExtension = extension,
                    PageCount = result.PageCount,
                    MergedImageUrl = result.MergedUrl
                };
            }
            catch (Exception ex)
            {
                // ERROR RETURN
                return new ProcessResult
                {
                    Success = false,
                    FileName = fileName,
                    FileExtension = extension,
                    ErrorMessage = ex.Message,
                    ErrorType = ex.GetType().Name
                };
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // ======================================================
        // PROCESS DOCX
        // ======================================================
        private async Task<(int PageCount, string MergedUrl)> ProcessDocx(string fileName, Stream stream)
        {
            var doc = new Document(stream);

            NormalizeMargins(doc);

            int pageCount = doc.PageCount;

            var pageImages = new List<byte[]>(pageCount);
            for (int i = 0; i < pageCount; i++)
                pageImages.Add(RenderPage(doc, i));

            var mergedBytes = MergeImagesVertically(pageImages);

            string url = await UploadMergedImage(fileName, mergedBytes);

            return (pageCount, url);
        }

        // ======================================================
        // NORMALIZE PAGE MARGINS
        // ======================================================
        private void NormalizeMargins(Document doc)
        {
            foreach (Section s in doc.Sections)
            {
                var ps = s.PageSetup;
                ps.TopMargin = 60;
                ps.BottomMargin = 60;
                ps.LeftMargin = 36;
                ps.RightMargin = 36;
                ps.HeaderDistance = 0;
                ps.FooterDistance = 0;
            }
        }

        // ======================================================
        // RENDER SINGLE PAGE → PNG
        // ======================================================
        private byte[] RenderPage(Document doc, int pageIndex)
        {
            var options = new ImageSaveOptions(SaveFormat.Png)
            {
                HorizontalResolution = 200,
                VerticalResolution = 200,
                UseHighQualityRendering = true,
                PaperColor = Color.White,
                Scale = 0.85f,
                PageSet = new PageSet(pageIndex)
            };

            using var ms = new MemoryStream();
            lock (doc)
            {
                doc.Save(ms, options);
            }
            return ms.ToArray();
        }

        // ======================================================
        // MERGE IMAGES VERTICALLY
        // ======================================================
        private byte[] MergeImagesVertically(List<byte[]> images)
        {
            var bitmaps = images.Select(b => new Bitmap(new MemoryStream(b))).ToList();

            int spacing = 50;
            int width = bitmaps.Max(b => b.Width);
            int height = bitmaps.Sum(b => b.Height) + spacing * (bitmaps.Count - 1);

            using var result = new Bitmap(width, height);
            using (var g = Graphics.FromImage(result))
            {
                g.Clear(Color.White);
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                int y = 0;
                foreach (var bmp in bitmaps)
                {
                    g.DrawImage(bmp, 0, y, bmp.Width, bmp.Height);
                    y += bmp.Height + spacing;
                }
            }

            foreach (var bmp in bitmaps) bmp.Dispose();

            using var output = new MemoryStream();
            result.Save(output, ImageFormat.Png);

            return output.ToArray();
        }

        // ======================================================
        // UPLOAD MERGED IMAGE TO CLOUDINARY
        // ======================================================
        private async Task<string> UploadMergedImage(string fileName, byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);

            var upload = new ImageUploadParams
            {
                File = new FileDescription($"{fileName}-merged.png", ms),
                Folder = "submissions/docx"
            };

            var result = await _cloudinary.UploadAsync(upload);
            return result.SecureUrl.ToString();
        }

        // ======================================================
        // FILE NAME VALIDATION
        // ======================================================
        private (bool IsValid, string Error) ValidateFileName(string nameWithoutExt)
        {
            if (string.IsNullOrWhiteSpace(nameWithoutExt))
                return (false, "File name cannot be empty.");

            if (nameWithoutExt == "0")
                return (false, "File name cannot be '0'.");

            if (nameWithoutExt.StartsWith("_"))
                return (false, "File name cannot start with '_'.");

            if (nameWithoutExt.Any(char.IsWhiteSpace))
                return (false, "File name cannot contain whitespace.");

            if (!nameWithoutExt.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                return (false, "File name contains invalid characters.");

            return (true, null);
        }

        // ======================================================
        // CLEAN RESPONSE DTO (AUTO-HIDE NULL FIELDS)
        // ======================================================
        public class ProcessResult
        {
            public bool Success { get; set; }

            public string FileName { get; set; }
            public string FileExtension { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public int? PageCount { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string MergedImageUrl { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string ErrorMessage { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string ErrorType { get; set; }
        }
    }
}
