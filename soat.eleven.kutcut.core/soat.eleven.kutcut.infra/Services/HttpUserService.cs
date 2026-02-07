using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using soat.eleven.kutcut.domain.Dtos;
using soat.eleven.kutcut.domain.Services;
using soat.eleven.kutcut.infra.Configuration;
using System.Text.Json;

namespace soat.eleven.kutcut.infra.Services
{
    public class HttpUserService : IUserSerivce
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AuthServiceSettings _settings;
        private readonly ILogger<HttpUserService> _logger;

        public HttpUserService(
            IHttpClientFactory httpClientFactory,
            IOptions<AuthServiceSettings> settings,
            ILogger<HttpUserService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        public UserDto GetUser(Guid userId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_settings.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

                var response = client.GetAsync($"/api/users/{userId}").Result;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get user {UserId}. Status: {StatusCode}", userId, response.StatusCode);
                    throw new HttpRequestException($"Failed to retrieve user {userId}. Status: {response.StatusCode}");
                }

                var content = response.Content.ReadAsStringAsync().Result;
                var user = JsonSerializer.Deserialize<UserDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (user == null)
                {
                    _logger.LogError("Failed to deserialize user {UserId}", userId);
                    throw new InvalidOperationException($"Failed to deserialize user {userId}");
                }

                _logger.LogInformation("Successfully retrieved user {UserId}: {Email}", userId, user.Email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", userId);
                throw;
            }
        }
    }
}
