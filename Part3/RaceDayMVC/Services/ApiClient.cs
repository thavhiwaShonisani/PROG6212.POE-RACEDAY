using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace RaceDayMVC.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        private readonly IHttpContextAccessor _ctx;
        public ApiClient(HttpClient http, IHttpContextAccessor ctx, IConfiguration cfg)
        {
            _http = http;
            _ctx = ctx;
            _http.BaseAddress = new Uri(cfg["ApiSettings:BaseUrl"]!);
        }
        private void AttachToken()
        {
            var token = _ctx.HttpContext?.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
        }
        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            AttachToken();
            return await _http.GetAsync(url);
        }
        public async Task<HttpResponseMessage> PostAsync(string url, object data)
        {
            AttachToken();
            var json = JsonSerializer.Serialize(data);
            return await _http.PostAsync(url,
                new StringContent(json, Encoding.UTF8, "application/json"));
        }
        public async Task<HttpResponseMessage> PutAsync(string url, object data)
        {
            AttachToken();
            var json = JsonSerializer.Serialize(data);
            return await _http.PutAsync(url,

                new StringContent(json, Encoding.UTF8, "application/json"));
        }
        public async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            AttachToken();
            return await _http.DeleteAsync(url);
        }
        public async Task<HttpResponseMessage> PostMultipartAsync(string url, IFormFile file, string
          field = "file")
        {
            AttachToken();
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(
                file.ContentType ?? "application/octet-stream");
            content.Add(fileContent, field, file.FileName);
            return await _http.PostAsync(url, content);
        }
    }
}


