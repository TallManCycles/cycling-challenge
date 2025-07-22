// ABOUTME: Service for handling Garmin OAuth 2.0 PKCE authentication flow
// ABOUTME: Manages token exchange, refresh, and PKCE verification
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CyclingChallenge.Models;
using CyclingChallenge.Data;
using Microsoft.EntityFrameworkCore;

namespace CyclingChallenge.Services;

public class GarminAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ChallengeDbContext _context;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    public GarminAuthService(HttpClient httpClient, ChallengeDbContext context)
    {
        _httpClient = httpClient;
        _context = context;
        _clientId = Environment.GetEnvironmentVariable("GARMIN_CLIENT_ID") ?? throw new InvalidOperationException("GARMIN_CLIENT_ID not configured");
        _clientSecret = Environment.GetEnvironmentVariable("GARMIN_CLIENT_SECRET") ?? throw new InvalidOperationException("GARMIN_CLIENT_SECRET not configured");
        _redirectUri = Environment.GetEnvironmentVariable("GARMIN_REDIRECT_URI") ?? throw new InvalidOperationException("GARMIN_REDIRECT_URI not configured");
    }

    public string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string GenerateCodeChallenge(string codeVerifier)
    {
        var bytes = Encoding.UTF8.GetBytes(codeVerifier);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string GetAuthUrl(string codeChallenge, string state)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["response_type"] = "code",
            ["state"] = state,
            ["redirect_uri"] = _redirectUri,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        return $"https://connect.garmin.com/oauth2Confirm?{query}";
    }

    public async Task<(string AccessToken, string RefreshToken, DateTime Expiry)> ExchangeCodeForTokensAsync(string code, string codeVerifier)
    {
        var tokenData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["redirect_uri"] = _redirectUri,
            ["code_verifier"] = codeVerifier
        };

        var content = new FormUrlEncodedContent(tokenData);
        var response = await _httpClient.PostAsync("https://connectapi.garmin.com/di-oauth2-service/oauth/token", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Token exchange failed: {response.StatusCode} - {error}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

        var accessToken = tokenResponse.GetProperty("access_token").GetString() ?? throw new Exception("No access token received");
        var refreshToken = tokenResponse.GetProperty("refresh_token").GetString() ?? throw new Exception("No refresh token received");
        var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();

        return (accessToken, refreshToken, DateTime.UtcNow.AddSeconds(expiresIn));
    }

    public async Task<string> GetGarminUserIdAsync(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        var response = await _httpClient.GetAsync("https://apis.garmin.com/wellness-api/rest/user/id");
        
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get Garmin user ID: {response.StatusCode}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var userResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
        
        return userResponse.GetProperty("userId").GetString() ?? throw new Exception("No user ID in response");
    }

    public async Task<User?> GetUserByGarminIdAsync(string garminUserId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.GarminUserId == garminUserId);
    }

    public async Task<User> CreateOrUpdateUserAsync(string garminUserId, string accessToken, string refreshToken, DateTime expiry, string email, string name)
    {
        var existingUser = await GetUserByGarminIdAsync(garminUserId);
        
        if (existingUser != null)
        {
            existingUser.GarminAccessToken = accessToken;
            existingUser.GarminRefreshToken = refreshToken;
            existingUser.TokenExpiry = expiry;
            existingUser.Name = name;
            existingUser.Email = email;
            
            _context.Users.Update(existingUser);
            await _context.SaveChangesAsync();
            return existingUser;
        }

        var newUser = new User
        {
            GarminUserId = garminUserId,
            GarminAccessToken = accessToken,
            GarminRefreshToken = refreshToken,
            TokenExpiry = expiry,
            Name = name,
            Email = email
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
        return newUser;
    }
}