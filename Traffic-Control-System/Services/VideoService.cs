using Microsoft.Extensions.Primitives;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;

namespace Traffic_Control_System.Services
{
    public interface IVideoService
    {
        public Task<bool> IsTokenExpired();
        public Task<string> GetToken();
        public Task<HttpResponseMessage> VideoServiceProxy(string URL);
    }
    public class VideoService : IVideoService
    {
        private HttpClient _httpClient;
        private string Token;
        private Uri _videoServiceURL;
        private readonly IConfiguration config;

        public VideoService(IConfiguration config)
        {
            _videoServiceURL = new Uri(config["VideoServiceURL"]);
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = _videoServiceURL;
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            this.config = config;

        }

        public async Task<bool> IsTokenExpired()
        {
            if (Token == null)
            {
                return true;
            }

            var handler = new JwtSecurityTokenHandler();
            var tokenS = handler.ReadToken(Token) as JwtSecurityToken;

            if (tokenS.ValidTo < DateTime.UtcNow.AddMinutes(-5))
            {
                return true;
            }

            return false;
        }

        public async Task<string> GetToken()
        {
            if (await IsTokenExpired())
            {
                var apiKey = WebUtility.UrlEncode(config["API_KEY"]);
                var param = $"userId={apiKey}";

                HttpResponseMessage response = await _httpClient.GetAsync($"Token/GetToken?{param}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    Token = JsonConvert.DeserializeObject<string>(responseContent);
                }
                else
                {
                    throw new Exception("Failed to get token");
                }
            }

            return Token;
        }

        public async Task<HttpResponseMessage> VideoServiceProxy(string URL)
        {
            var token = await GetToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("/" + URL);

            response.RequestMessage.Headers.Authorization = new AuthenticationHeaderValue("No", "No");

            return response;
        }
    }
}
