using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PRN232.Final.ExamEval.Repositories.Entities;
using PRN232.Final.ExamEval.Services.IServices;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PRN232.Final.ExamEval.Services.Services
{
    public class FileService : IFileService
    {
        private readonly string _firebaseDbUrl;
        private readonly GoogleCredential _credential;
        private readonly HttpClient _httpClient;

        public FileService(string firebaseDbUrl, string serviceAccountPath)
        {
            _firebaseDbUrl = firebaseDbUrl;
            _credential = GoogleCredential.FromFile(serviceAccountPath)
                .CreateScoped("https://www.googleapis.com/auth/firebase.database",
                              "https://www.googleapis.com/auth/userinfo.email");
            _httpClient = new HttpClient();
        }
        

        public async Task UploadZipAsync(IFormFile zipFile)
        {
            using var stream = zipFile.OpenReadStream();
            using var archive = ArchiveFactory.Open(stream);

            var token = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

            foreach (var entry in archive.Entries)
            {
                if (!entry.IsDirectory) continue;

                string folderName = entry.Key.TrimEnd('/');

                // Kiểm tra folder tồn tại chưa
                var checkUrl = $"{_firebaseDbUrl}/Folders/{folderName}.json?access_token={token}";
                var existing = await _httpClient.GetAsync(checkUrl);
                if (existing.IsSuccessStatusCode)
                {
                    var existingValue = await existing.Content.ReadAsStringAsync();
                    if (existingValue != "null")
                        continue; // Folder đã tồn tại
                }

                var content = new { uploadedAt = DateTime.UtcNow };
                await _httpClient.PutAsJsonAsync(checkUrl, content);
            }
        }

    }
}
