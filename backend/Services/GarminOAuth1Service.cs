// ABOUTME: Garmin OAuth 1.0 service matching the working Node.js implementation
// ABOUTME: Uses the older Garmin API with request tokens and HMAC-SHA1 signing
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CyclingChallenge.Models;
using CyclingChallenge.Data;
using Microsoft.EntityFrameworkCore;

namespace CyclingChallenge.Services;

public class GarminOAuth1Service
{
    private readonly HttpClient _httpClient;
    private readonly ChallengeDbContext _context;
    private readonly ChallengeService _challengeService;
    private readonly string _consumerKey;
    private readonly string _consumerSecret;
    private readonly string _requestTokenUrl;
    private readonly string _accessTokenUrl;
    private readonly string _authorizeUrl;
    private readonly string _callbackUrl;

    public GarminOAuth1Service(HttpClient httpClient, ChallengeDbContext context, ChallengeService challengeService)
    {
        _httpClient = httpClient;
        _context = context;
        _challengeService = challengeService;
        
        // OAuth 1.0 endpoints (not OAuth 2.0)
        _consumerKey = Environment.GetEnvironmentVariable("GARMIN_CONSUMER_KEY") 
            ?? throw new InvalidOperationException("GARMIN_CONSUMER_KEY not configured");
        _consumerSecret = Environment.GetEnvironmentVariable("GARMIN_CONSUMER_SECRET") 
            ?? throw new InvalidOperationException("GARMIN_CONSUMER_SECRET not configured");
        _requestTokenUrl = Environment.GetEnvironmentVariable("REQUEST_TOKEN_URL") 
            ?? "https://connectapi.garmin.com/oauth-service/oauth/request_token";
        _accessTokenUrl = Environment.GetEnvironmentVariable("ACCESS_TOKEN_URL") 
            ?? "https://connectapi.garmin.com/oauth-service/oauth/access_token";
        _authorizeUrl = Environment.GetEnvironmentVariable("AUTHORIZE_URL") 
            ?? "https://connect.garmin.com/oauthConfirm";
        _callbackUrl = Environment.GetEnvironmentVariable("GARMIN_CALLBACK_URL") 
            ?? $"{Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "https://localhost:3000"}/api/garmin-callback";
    }

    public async Task<(string RequestToken, string RequestTokenSecret, string AuthorizeUrl)> GetRequestTokenAsync()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");

        var parameters = new SortedDictionary<string, string>
        {
            ["oauth_callback"] = _callbackUrl,
            ["oauth_consumer_key"] = _consumerKey,
            ["oauth_nonce"] = nonce,
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_timestamp"] = timestamp,
            ["oauth_version"] = "1.0"
        };

        var signature = GenerateSignature("POST", _requestTokenUrl, parameters, _consumerSecret, "");
        parameters["oauth_signature"] = signature;

        var authHeader = "OAuth " + string.Join(", ", parameters.Select(kvp => 
            $"{Uri.EscapeDataString(kvp.Key)}=\"{Uri.EscapeDataString(kvp.Value)}\""));

        var request = new HttpRequestMessage(HttpMethod.Post, _requestTokenUrl);
        request.Headers.Add("Authorization", authHeader);
        request.Headers.Add("Accept", "*/*");
        request.Headers.Add("Connection", "close");
        request.Headers.Add("User-Agent", "CyclingChallenge/1.0");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"=== REQUEST TOKEN DEBUG ===");
        Console.WriteLine($"Request URL: {_requestTokenUrl}");
        Console.WriteLine($"Response Status: {response.StatusCode}");
        Console.WriteLine($"Response Body: {responseBody}");
        Console.WriteLine($"Auth Header: {authHeader}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get request token: {response.StatusCode} - {responseBody}");
        }

        if (string.IsNullOrEmpty(responseBody))
        {
            throw new Exception("Empty response from Garmin request token endpoint");
        }

        var responseParams = ParseOAuthResponse(responseBody);
        
        if (!responseParams.ContainsKey("oauth_token") || !responseParams.ContainsKey("oauth_token_secret"))
        {
            throw new Exception($"Invalid response format: {responseBody}");
        }

        var requestToken = responseParams["oauth_token"];
        var requestTokenSecret = responseParams["oauth_token_secret"];
        var authorizeUrl = $"{_authorizeUrl}?oauth_token={requestToken}";

        Console.WriteLine($"Generated Request Token: {requestToken}");
        Console.WriteLine($"Authorize URL: {authorizeUrl}");

        return (requestToken, requestTokenSecret, authorizeUrl);
    }

    public async Task<(string AccessToken, string AccessTokenSecret)> GetAccessTokenAsync(
        string requestToken, string requestTokenSecret, string verifier)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");

        var parameters = new SortedDictionary<string, string>
        {
            ["oauth_consumer_key"] = _consumerKey,
            ["oauth_nonce"] = nonce,
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_timestamp"] = timestamp,
            ["oauth_token"] = requestToken,
            ["oauth_verifier"] = verifier,
            ["oauth_version"] = "1.0"
        };

        var signature = GenerateSignature("POST", _accessTokenUrl, parameters, _consumerSecret, requestTokenSecret);
        parameters["oauth_signature"] = signature;

        var authHeader = "OAuth " + string.Join(", ", parameters.Select(kvp => 
            $"{Uri.EscapeDataString(kvp.Key)}=\"{Uri.EscapeDataString(kvp.Value)}\""));

        var request = new HttpRequestMessage(HttpMethod.Post, _accessTokenUrl);
        request.Headers.Add("Authorization", authHeader);
        request.Headers.Add("Accept", "*/*");
        request.Headers.Add("Connection", "close");
        request.Headers.Add("User-Agent", "CyclingChallenge/1.0");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get access token: {response.StatusCode} - {responseBody}");
        }

        var responseParams = ParseOAuthResponse(responseBody);
        return (responseParams["oauth_token"], responseParams["oauth_token_secret"]);
    }

    private string GenerateSignature(string httpMethod, string url, 
        SortedDictionary<string, string> parameters, string consumerSecret, string tokenSecret)
    {
        var parameterString = string.Join("&", parameters.Select(kvp => 
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        var signatureBaseString = $"{httpMethod.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(parameterString)}";
        var signingKey = $"{Uri.EscapeDataString(consumerSecret)}&{Uri.EscapeDataString(tokenSecret)}";

        using var hmac = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey));
        var signatureBytes = hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBaseString));
        return Convert.ToBase64String(signatureBytes);
    }

    private Dictionary<string, string> ParseOAuthResponse(string response)
    {
        return response.Split('&')
            .Select(param => param.Split('='))
            .ToDictionary(
                parts => Uri.UnescapeDataString(parts[0]), 
                parts => Uri.UnescapeDataString(parts[1])
            );
    }

    public async Task<User> CreateOrUpdateUserAsync(string accessToken, string accessTokenSecret, string email, string name)
    {
        // For OAuth 1.0, we don't have a separate user ID endpoint
        // We'll use the access token as the identifier
        var garminUserId = accessToken.Substring(0, Math.Min(20, accessToken.Length)); // Use part of token as ID

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.GarminUserId == garminUserId);
        
        if (existingUser != null)
        {
            existingUser.GarminAccessToken = accessToken;
            existingUser.GarminRefreshToken = accessTokenSecret; // Store token secret in refresh token field
            existingUser.TokenExpiry = DateTime.UtcNow.AddYears(1); // OAuth 1.0 tokens don't expire
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
            GarminRefreshToken = accessTokenSecret,
            TokenExpiry = DateTime.UtcNow.AddYears(1),
            Name = name,
            Email = email
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();
        
        // Activate any challenges waiting for this user
        await _challengeService.ActivateWaitingChallengesForUserAsync(newUser.Id);
        
        return newUser;
    }

}