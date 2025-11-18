using Aspose.Words;
using Aspose.Words.Saving;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;

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

        // ------------------------------------------------------------
        // UPLOAD ZIP → EXTRACT → PROCESS EACH DOCX IN PARALLEL (SAFE)
        // ------------------------------------------------------------
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocxSubmission(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("ZIP file required.");

            if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only ZIP file allowed.");

            var docxFiles = new List<(string Name, byte[] Content)>();

            using (var zip = new ZipArchive(file.OpenReadStream()))
            {
                foreach (var entry in zip.Entries)
                {
                    if (!entry.FullName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                        continue;

                    using var ms = new MemoryStream();
                    await entry.Open().CopyToAsync(ms);
                    docxFiles.Add((entry.Name, ms.ToArray()));
                }
            }

            if (docxFiles.Count == 0)
                return BadRequest("No DOCX files found.");

            var tasks = docxFiles
                .Select(e => ProcessDocxWithSemaphore(e.Name, e.Content));

            var results = await Task.WhenAll(tasks);

            return Ok(new { totalFiles = results.Length, files = results });
        }

        // ------------------------------------------------------------
        // LIMIT DOCX PROCESSING PARALLELISM
        // ------------------------------------------------------------
        private async Task<object> ProcessDocxWithSemaphore(string fileName, byte[] bytes)
        {
            await _semaphore.WaitAsync();
            try
            {
                using var stream = new MemoryStream(bytes);
                return await ProcessDocx(fileName, stream);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // ------------------------------------------------------------
        // PROCESS EACH DOCX (THREAD SAFE)
        // ------------------------------------------------------------
        private async Task<object> ProcessDocx(string fileName, Stream stream)
        {
            var doc = new Document(stream);

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

            int pageCount = doc.PageCount;

            // Render tuần tự (Aspose yêu cầu)
            var pageImages = new List<byte[]>(capacity: pageCount);

            for (int page = 0; page < pageCount; page++)
            {
                pageImages.Add(RenderPageSafe(doc, page));
            }

            byte[] merged = MergePages(pageImages);
            string url = await UploadMerged(fileName, merged);

            return new
            {
                fileName,
                pageCount,
                mergedImage = url
            };
        }

        // ------------------------------------------------------------
        // SAFEST WAY TO RENDER 1 PAGE
        // ------------------------------------------------------------
        private byte[] RenderPageSafe(Document sourceDoc, int pageIndex)
        {
            var doc = sourceDoc.Clone();

            var options = new ImageSaveOptions(SaveFormat.Png)
            {
                HorizontalResolution = 200,
                VerticalResolution = 200,
                UseHighQualityRendering = true,
                PaperColor = Color.White,
                Scale = 0.85f,
                PageSet = new PageSet(pageIndex) // ⭐ FIX QUAN TRỌNG
            };

            using var ms = new MemoryStream();
            doc.Save(ms, options);
            return ms.ToArray();
        }

        // ------------------------------------------------------------
        // MERGE BITMAP VERTICALLY (HIỆU NĂNG CAO)
        // ------------------------------------------------------------
        private byte[] MergePages(List<byte[]> pages)
        {
            var bitmaps = pages.Select(b => new Bitmap(new MemoryStream(b))).ToList();

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

            foreach (var bmp in bitmaps)
                bmp.Dispose();

            using var outStream = new MemoryStream();
            result.Save(outStream, ImageFormat.Png);
            return outStream.ToArray();
        }

        // ------------------------------------------------------------
        // UPLOAD TO CLOUDINARY
        // ------------------------------------------------------------
        private async Task<string> UploadMerged(string fileName, byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);

            var upload = new ImageUploadParams
            {
                File = new FileDescription($"{fileName}-merged.png", ms),
                Folder = "submission-docx"
            };

            var result = await _cloudinary.UploadAsync(upload);
            return result.SecureUrl.ToString();
        }
    }
}
