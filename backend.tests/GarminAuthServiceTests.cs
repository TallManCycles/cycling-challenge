// ABOUTME: Unit tests for the GarminAuthService class
// ABOUTME: Tests PKCE code generation and verification logic
using CyclingChallenge.Services;
using CyclingChallenge.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace CyclingChallenge.Tests;

public class GarminAuthServiceTests : IDisposable
{
    private readonly ChallengeDbContext _context;
    private readonly GarminAuthService _service;
    private readonly HttpClient _httpClient;

    public GarminAuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChallengeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChallengeDbContext(options);
        _httpClient = new HttpClient();
        
        // Set environment variables for testing
        Environment.SetEnvironmentVariable("GARMIN_CLIENT_ID", "test_client_id");
        Environment.SetEnvironmentVariable("GARMIN_CLIENT_SECRET", "test_client_secret");
        Environment.SetEnvironmentVariable("GARMIN_REDIRECT_URI", "http://localhost:5173/auth/callback");
        
        _service = new GarminAuthService(_httpClient, _context);
    }

    [Fact]
    public void GenerateCodeVerifier_ShouldReturnValidBase64UrlString()
    {
        // Act
        var codeVerifier = _service.GenerateCodeVerifier();

        // Assert
        Assert.NotNull(codeVerifier);
        Assert.True(codeVerifier.Length >= 43 && codeVerifier.Length <= 128);
        Assert.DoesNotContain(codeVerifier, c => c == '=' || c == '+' || c == '/');
    }

    [Fact]
    public void GenerateCodeChallenge_ShouldReturnCorrectSHA256Hash()
    {
        // Arrange
        var codeVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

        // Act
        var codeChallenge = _service.GenerateCodeChallenge(codeVerifier);

        // Assert
        Assert.NotNull(codeChallenge);
        
        // Verify it's a proper SHA256 hash in base64url format
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        var expectedChallenge = Convert.ToBase64String(expectedHash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
            
        Assert.Equal(expectedChallenge, codeChallenge);
    }

    [Fact]
    public void GetAuthUrl_ShouldReturnValidGarminUrl()
    {
        // Arrange
        var codeChallenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";
        var state = "test_state_123";

        // Act
        var authUrl = _service.GetAuthUrl(codeChallenge, state);

        // Assert
        Assert.StartsWith("https://connect.garmin.com/oauth2/authorize", authUrl);
        Assert.Contains("response_type=code", authUrl);
        Assert.Contains("client_id=test_client_id", authUrl);
        Assert.Contains("scope=activity%3Aread", authUrl);
        Assert.Contains($"code_challenge={codeChallenge}", authUrl);
        Assert.Contains("code_challenge_method=S256", authUrl);
        Assert.Contains($"state={state}", authUrl);
    }

    [Fact]
    public async Task CreateOrUpdateUserAsync_ShouldCreateNewUser_WhenNotExists()
    {
        // Arrange
        var garminUserId = "test_garmin_123";
        var accessToken = "access_token_123";
        var refreshToken = "refresh_token_123";
        var expiry = DateTime.UtcNow.AddHours(1);
        var email = "test@example.com";
        var name = "Test User";

        // Act
        var user = await _service.CreateOrUpdateUserAsync(garminUserId, accessToken, refreshToken, expiry, email, name);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(garminUserId, user.GarminUserId);
        Assert.Equal(accessToken, user.GarminAccessToken);
        Assert.Equal(refreshToken, user.GarminRefreshToken);
        Assert.Equal(email, user.Email);
        Assert.Equal(name, user.Name);
        
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.GarminUserId == garminUserId);
        Assert.NotNull(savedUser);
    }

    [Fact]
    public async Task CreateOrUpdateUserAsync_ShouldUpdateExistingUser_WhenExists()
    {
        // Arrange
        var existingUser = new CyclingChallenge.Models.User
        {
            GarminUserId = "existing_garmin_123",
            Name = "Old Name",
            Email = "old@example.com",
            GarminAccessToken = "old_token",
            GarminRefreshToken = "old_refresh"
        };
        
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var newAccessToken = "new_access_token";
        var newRefreshToken = "new_refresh_token";
        var newExpiry = DateTime.UtcNow.AddHours(2);
        var newEmail = "new@example.com";
        var newName = "New Name";

        // Act
        var updatedUser = await _service.CreateOrUpdateUserAsync(
            "existing_garmin_123", newAccessToken, newRefreshToken, newExpiry, newEmail, newName);

        // Assert
        Assert.Equal(existingUser.Id, updatedUser.Id);
        Assert.Equal(newAccessToken, updatedUser.GarminAccessToken);
        Assert.Equal(newRefreshToken, updatedUser.GarminRefreshToken);
        Assert.Equal(newEmail, updatedUser.Email);
        Assert.Equal(newName, updatedUser.Name);
    }

    public void Dispose()
    {
        _context.Dispose();
        _httpClient.Dispose();
    }
}