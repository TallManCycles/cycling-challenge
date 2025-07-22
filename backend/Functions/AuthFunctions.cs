// ABOUTME: Azure Functions for handling Garmin OAuth authentication flow
// ABOUTME: Includes start auth endpoint and callback handler with PKCE validation
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CyclingChallenge.Services;
using CyclingChallenge.Data;

namespace CyclingChallenge.Functions;

public class AuthFunctions
{
    private readonly ILogger<AuthFunctions> _logger;
    private readonly GarminAuthService _authService;
    private readonly ChallengeDbContext _context;

    public AuthFunctions(ILogger<AuthFunctions> logger, GarminAuthService authService, ChallengeDbContext context)
    {
        _logger = logger;
        _authService = authService;
        _context = context;
    }

    [Function("StartGarminAuthOAuth2_DISABLED")]
    public async Task<HttpResponseData> StartGarminAuth([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/garmin/start-oauth2")] HttpRequestData req)
    {
        _logger.LogInformation("Starting Garmin OAuth flow");

        try
        {
            var codeVerifier = _authService.GenerateCodeVerifier();
            var codeChallenge = _authService.GenerateCodeChallenge(codeVerifier);
            var state = Guid.NewGuid().ToString();

            var authUrl = _authService.GetAuthUrl(codeChallenge, state);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                authUrl,
                codeVerifier,
                state
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Garmin auth");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("GarminAuthCallbackOAuth2_DISABLED")]
    public async Task<HttpResponseData> GarminAuthCallback([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/garmin/callback-oauth2")] HttpRequestData req)
    {
        _logger.LogInformation("Processing Garmin OAuth callback");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var callbackData = JsonSerializer.Deserialize<JsonElement>(requestBody ?? "{}");

            var code = callbackData.GetProperty("code").GetString();
            var state = callbackData.GetProperty("state").GetString();
            var codeVerifier = callbackData.GetProperty("codeVerifier").GetString();
            var email = callbackData.GetProperty("email").GetString();
            var name = callbackData.GetProperty("name").GetString();

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(codeVerifier) || 
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(name))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Missing required parameters");
                return badResponse;
            }

            var (accessToken, refreshToken, expiry) = await _authService.ExchangeCodeForTokensAsync(code, codeVerifier);
            var garminUserId = await _authService.GetGarminUserIdAsync(accessToken);

            var user = await _authService.CreateOrUpdateUserAsync(garminUserId, accessToken, refreshToken, expiry, email, name);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                userId = user.Id,
                name = user.Name,
                email = user.Email,
                success = true
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Garmin callback");
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
            var garminUserId = query["garminUserId"];

            if (string.IsNullOrEmpty(garminUserId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Missing garminUserId parameter");
                return badResponse;
            }

            var user = await _authService.GetUserByGarminIdAsync(garminUserId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"User {garminUserId} successfully deregistered");
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