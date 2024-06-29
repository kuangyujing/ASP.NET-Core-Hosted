using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorApp.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Http;

namespace BlazorApp.Server.Services {
    public class DataverseService {
        private readonly ILogger<DataverseService> _logger;
        private readonly IPublicClientApplication _authBuilder;
        private readonly string[] _scopes;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DataverseService(ILogger<DataverseService> logger, HttpClient httpClient, IHttpContextAccessor httpContextAccessor) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            string resource = "https://6e450aad.api.crm7.dynamics.com";
            var clientId = "51f81489-12ee-4a9e-aaae-a2591f45987d";
            var redirectUri = "http://localhost";

            _authBuilder = PublicClientApplicationBuilder.Create(clientId)
                            .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                            .WithRedirectUri(redirectUri)
                            .Build();
            _scopes = new[] { resource + "/user_impersonation" };

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.BaseAddress = new Uri("https://org22ddaee2.api.crm7.dynamics.com/api/data/v9.2/");
            _httpClient.Timeout = new TimeSpan(0, 2, 0);

            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public async Task<Guid> FetchUserId() {
            AuthenticationResult token = await GetTokenAsync();
            if (token == null) {
                return Guid.Empty;
            }

            HttpRequestHeaders headers = _httpClient.DefaultRequestHeaders;
            headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            headers.Add("OData-MaxVersion", "4.0");
            headers.Add("OData-Version", "4.0");
            headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.GetAsync("WhoAmI");

            if (response.IsSuccessStatusCode) {
                string jsonContent = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(jsonContent)) {
                    JsonElement root = doc.RootElement;
                    JsonElement userIdElement = root.GetProperty("UserId");
                    var userId = userIdElement.GetGuid();
                    _logger.LogInformation($"User ID: {userId}");
                    return userId;
                }
            }
            else {
                _logger.LogError($"Web API call failed. Reason: {response.ReasonPhrase}");
                return Guid.Empty;
            }
        }

        private async Task<AuthenticationResult> GetTokenAsync() {
            // Check if token is in the cookies
            var request = _httpContextAccessor.HttpContext.Request;
            var response = _httpContextAccessor.HttpContext.Response;
            if (request.Cookies.TryGetValue("AuthToken", out string tokenJson)) {
                var token = JsonSerializer.Deserialize<AuthenticationResult>(tokenJson);
                if (token != null && token.ExpiresOn > DateTimeOffset.Now) {
                    _logger.LogInformation("Token acquired from cookies.");
                    return token;
                }
            }

            // Acquire token interactively or silently
            AuthenticationResult tokenResult;
            try {
                var accounts = await _authBuilder.GetAccountsAsync();
                tokenResult = await _authBuilder.AcquireTokenSilent(_scopes, accounts.FirstOrDefault()).ExecuteAsync();
                _logger.LogInformation("Token acquired silently.");
            }
            catch (MsalUiRequiredException) {
                tokenResult = await _authBuilder.AcquireTokenInteractive(_scopes).ExecuteAsync();
                _logger.LogInformation("Token acquired interactively.");
            }

            // Save the token in cookies
            tokenJson = JsonSerializer.Serialize(tokenResult);
            var cookieOptions = new CookieOptions {
                HttpOnly = true,
                Expires = tokenResult.ExpiresOn.DateTime
            };
            response.Cookies.Append("AuthToken", tokenJson, cookieOptions);

            return tokenResult;
        }
    }
}
