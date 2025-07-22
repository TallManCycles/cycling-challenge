// ABOUTME: Unit tests for the GarminOAuth1Service class
// ABOUTME: Tests OAuth 1.0 signature generation and token handling
using CyclingChallenge.Services;
using CyclingChallenge.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CyclingChallenge.Tests;

public class GarminOAuth1ServiceTests : IDisposable
{
    private readonly ChallengeDbContext _context;
    private readonly GarminOAuth1Service _service;
    private readonly HttpClient _httpClient;
    private readonly ChallengeService _challengeService;

    public GarminOAuth1ServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChallengeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChallengeDbContext(options);
        _httpClient = new HttpClient();
        _challengeService = new ChallengeService(_context);
        
        // Set environment variables for testing
        Environment.SetEnvironmentVariable("GARMIN_CONSUMER_KEY", "test_consumer_key");
        Environment.SetEnvironmentVariable("GARMIN_CONSUMER_SECRET", "test_consumer_secret");
        Environment.SetEnvironmentVariable("FRONTEND_URL", "https://test.example.com");
        Environment.SetEnvironmentVariable("REQUEST_TOKEN_URL", "https://connectapi.garmin.com/oauth-service/oauth/request_token");
        Environment.SetEnvironmentVariable("ACCESS_TOKEN_URL", "https://connectapi.garmin.com/oauth-service/oauth/access_token");
        Environment.SetEnvironmentVariable("AUTHORIZE_URL", "https://connect.garmin.com/oauthConfirm");
        
        _service = new GarminOAuth1Service(_httpClient, _context, _challengeService);
    }

    [Fact]
    public async Task CreateOrUpdateUserAsync_ShouldCreateNewUser_WhenNotExists()
    {
        // Arrange
        var accessToken = "test_access_token_123";
        var accessTokenSecret = "test_access_token_secret_123";
        var email = "test@example.com";
        var name = "Test User";

        // Act
        var user = await _service.CreateOrUpdateUserAsync(accessToken, accessTokenSecret, email, name);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(accessToken, user.GarminAccessToken);
        Assert.Equal(accessTokenSecret, user.GarminRefreshToken); // OAuth 1.0 stores secret in refresh token field
        Assert.Equal(email, user.Email);
        Assert.Equal(name, user.Name);
        Assert.NotNull(user.GarminUserId);
        
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.GarminUserId == user.GarminUserId);
        Assert.NotNull(savedUser);
    }

    [Fact]
    public async Task CreateOrUpdateUserAsync_ShouldUpdateExistingUser_WhenExists()
    {
        // Arrange
        var accessToken = "existing_token_123456789012345";
        var garminUserId = accessToken.Substring(0, Math.Min(20, accessToken.Length)); // Same logic as service
        
        var existingUser = new CyclingChallenge.Models.User
        {
            GarminUserId = garminUserId,
            Name = "Old Name",
            Email = "old@example.com",
            GarminAccessToken = accessToken,
            GarminRefreshToken = "old_secret"
        };
        
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var newAccessToken = accessToken; // Same token to match existing user
        var newAccessTokenSecret = "new_secret";
        var newEmail = "new@example.com";
        var newName = "New Name";

        // Act
        var updatedUser = await _service.CreateOrUpdateUserAsync(
            newAccessToken, newAccessTokenSecret, newEmail, newName);

        // Assert
        Assert.Equal(existingUser.Id, updatedUser.Id);
        Assert.Equal(newAccessToken, updatedUser.GarminAccessToken);
        Assert.Equal(newAccessTokenSecret, updatedUser.GarminRefreshToken);
        Assert.Equal(newEmail, updatedUser.Email);
        Assert.Equal(newName, updatedUser.Name);
    }

    public void Dispose()
    {
        _context.Dispose();
        _httpClient.Dispose();
    }
}