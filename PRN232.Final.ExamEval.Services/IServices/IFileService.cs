using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Services.IServices
{
    public interface IFileService
    {
        Task UploadZipAsync(IFormFile zipFile);
    }
}
