using PRN232.Final.ExamEval.FE.Models;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace PRN232.Final.ExamEval.FE.Services
{
    internal class AuthService
    {
        private readonly HttpClient _client;

        public AuthService()
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:5000/");
        }

        public async Task<string?> LoginAsync(string email, string password)
        {
            var req = new UserForAuthenticationRequest
            {
                Email = email,
                Password = password
            };

            string json = JsonConvert.SerializeObject(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("api/auth/login", content);

            if (!response.IsSuccessStatusCode)
                return null;

            string result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<LoginResponse>(result)?.token;
        }

        public async Task<string?> GetCurrentUserAsync(string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/current-user");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }
    }
}
