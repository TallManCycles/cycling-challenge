// ABOUTME: Azure Functions for Garmin OAuth 1.0 authentication flow
// ABOUTME: Matches the working Node.js OAuth 1.0 implementation with request tokens
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CyclingChallenge.Services;
using CyclingChallenge.Data;
using Microsoft.EntityFrameworkCore;

namespace CyclingChallenge.Functions;

public class GarminOAuth1Functions
{
    private readonly ILogger<GarminOAuth1Functions> _logger;
    private readonly GarminOAuth1Service _authService;
    private readonly ChallengeDbContext _context;
    private readonly string _frontendUrl;

    // Simple in-memory storage for request tokens (for 2-user app this is fine)
    private static readonly Dictionary<string, (string Secret, string UserId)> _requestTokens = new();
    
    // Store user tokens temporarily until name is provided
    private static readonly Dictionary<string, (string accessToken, string accessTokenSecret)> _tempUserTokens = new();

    public GarminOAuth1Functions(ILogger<GarminOAuth1Functions> logger, GarminOAuth1Service authService, ChallengeDbContext context)
    {
        _logger = logger;
        _authService = authService;
        _context = context;
        _frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") 
            ?? "https://b822059bd803.ngrok-free.app";
    }

    [Function("StartGarminOAuth1")]
    public async Task<HttpResponseData> StartGarminOAuth1([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/garmin/start")] HttpRequestData req)
    {
        _logger.LogInformation("=== Starting Garmin OAuth 1.0 flow ===");
        _logger.LogInformation("Request URL: {RequestUrl}", req.Url);

        try
        {
            // Get user ID from query params (you'll need to pass this from frontend)
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var userId = query["userId"] ?? "temp-user"; // Temporary user ID
            
            _logger.LogInformation("Processing OAuth start for user: {UserId}", userId);

            var (requestToken, requestTokenSecret, authorizeUrl) = await _authService.GetRequestTokenAsync();

            _logger.LogInformation("Generated request token: {RequestToken}", requestToken[..8] + "...");
            _logger.LogInformation("Generated authorize URL: {AuthorizeUrl}", authorizeUrl);

            // Store request token and secret temporarily
            _requestTokens[requestToken] = (requestTokenSecret, userId);
            _logger.LogInformation("Stored request token for user: {UserId}. Active tokens: {TokenCount}", 
                userId, _requestTokens.Count);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                authUrl = authorizeUrl,
                requestToken = requestToken
            });

            _logger.LogInformation("OAuth 1.0 start completed successfully for user: {UserId}", userId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Garmin OAuth 1.0 for request: {RequestUrl}", req.Url);
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("GarminOAuth1Callback")]
    public async Task<HttpResponseData> GarminOAuth1Callback([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "garmin-callback")] HttpRequestData req)
    {
        _logger.LogInformation("Processing Garmin OAuth 1.0 callback");

        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var oauthToken = query["oauth_token"];
            var oauthVerifier = query["oauth_verifier"];

            if (string.IsNullOrEmpty(oauthToken) || string.IsNullOrEmpty(oauthVerifier))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Missing OAuth parameters");
                return badResponse;
            }

            // Retrieve stored request token secret
            if (!_requestTokens.TryGetValue(oauthToken, out var tokenInfo))
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await notFoundResponse.WriteStringAsync("Request token not found");
                return notFoundResponse;
            }

            var (requestTokenSecret, userId) = tokenInfo;

            // Exchange for access token
            var (accessToken, accessTokenSecret) = await _authService.GetAccessTokenAsync(
                oauthToken, requestTokenSecret, oauthVerifier);

            // Clean up stored request token
            _requestTokens.Remove(oauthToken);

            // Redirect to frontend name entry form since Garmin API doesn't provide user names
            // Store tokens temporarily until user completes name entry
            var tempUserId = Guid.NewGuid().ToString();
            _tempUserTokens[tempUserId] = (accessToken, accessTokenSecret);

            // Redirect to frontend name entry page
            var response = req.CreateResponse(HttpStatusCode.Redirect);
            response.Headers.Add("Location", $"{_frontendUrl}/auth/name-entry?tempUserId={tempUserId}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Garmin OAuth 1.0 callback");
            
            // Redirect to frontend with error
            var response = req.CreateResponse(HttpStatusCode.Redirect);
            response.Headers.Add("Location", $"{_frontendUrl}/auth/callback?error=oauth_failed");
            return response;
        }
    }

    [Function("CompleteUserRegistration")]
    public async Task<HttpResponseData> CompleteUserRegistration([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/complete-registration")] HttpRequestData req)
    {
        _logger.LogInformation("Completing user registration with name");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var registrationData = JsonSerializer.Deserialize<JsonElement>(requestBody ?? "{}");

            var tempUserId = registrationData.GetProperty("tempUserId").GetString();
            var name = registrationData.GetProperty("name").GetString();
            var email = registrationData.TryGetProperty("email", out var emailElement) 
                ? emailElement.GetString() ?? $"{name.Replace(" ", "").ToLower()}@user.local"
                : $"{name.Replace(" ", "").ToLower()}@user.local";

            if (string.IsNullOrEmpty(tempUserId) || !_tempUserTokens.TryGetValue(tempUserId, out var tokenInfo))
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await notFoundResponse.WriteStringAsync("Invalid or expired registration token");
                return notFoundResponse;
            }

            var (accessToken, accessTokenSecret) = tokenInfo;

            // Create user with the provided name
            var user = await _authService.CreateOrUpdateUserAsync(accessToken, accessTokenSecret, email, name);

            // Clean up temporary token storage
            _tempUserTokens.Remove(tempUserId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email
                }
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing user registration");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("DeleteUserRegistration")]
    public async Task<HttpResponseData> DeleteUserRegistration([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/registration")] HttpRequestData req)
    {
        _logger.LogInformation("Processing user deregistration request");

        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var userId = query["userId"];

            if (string.IsNullOrEmpty(userId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Missing userId parameter");
                return badResponse;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.GarminUserId == userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"User {userId} successfully deregistered");
            }

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user deregistration");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}