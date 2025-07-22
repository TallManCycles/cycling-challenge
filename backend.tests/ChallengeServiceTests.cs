// ABOUTME: Unit tests for the ChallengeService class
// ABOUTME: Tests challenge creation, progress calculation, and business logic
using Microsoft.EntityFrameworkCore;
using CyclingChallenge.Data;
using CyclingChallenge.Models;
using CyclingChallenge.Services;
using Xunit;

namespace CyclingChallenge.Tests;

public class ChallengeServiceTests : IDisposable
{
    private readonly ChallengeDbContext _context;
    private readonly ChallengeService _service;

    public ChallengeServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChallengeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChallengeDbContext(options);
        _service = new ChallengeService(_context);
    }

    [Fact]
    public async Task CreateChallengeAsync_ShouldCreateChallenge_WithCorrectProperties()
    {
        // Arrange
        var creator = new User { Id = 1, Name = "Creator", Email = "creator@test.com", GarminUserId = "garmin1" };
        var opponent = new User { Id = 2, Name = "Opponent", Email = "opponent@test.com", GarminUserId = "garmin2" };
        
        _context.Users.AddRange(creator, opponent);
        await _context.SaveChangesAsync();

        // Act
        var challenge = await _service.CreateChallengeAsync(1, 2, "Test Challenge", ChallengeType.Distance, 100);

        // Assert
        Assert.NotNull(challenge);
        Assert.Equal("Test Challenge", challenge.Name);
        Assert.Equal(ChallengeType.Distance, challenge.Type);
        Assert.Equal(ChallengeStatus.Pending, challenge.Status);
        Assert.Equal(100, challenge.TargetValue);
        Assert.Equal(1, challenge.CreatorId);
        Assert.Equal(2, challenge.OpponentId);
    }

    [Fact]
    public async Task AcceptChallengeAsync_ShouldChangeStatusToActive()
    {
        // Arrange
        var creator = new User { Id = 1, Name = "Creator", Email = "creator@test.com", GarminUserId = "garmin1" };
        var opponent = new User { Id = 2, Name = "Opponent", Email = "opponent@test.com", GarminUserId = "garmin2" };
        
        _context.Users.AddRange(creator, opponent);
        
        var challenge = new Challenge
        {
            Id = 1,
            Name = "Test Challenge",
            Type = ChallengeType.Distance,
            Status = ChallengeStatus.Pending,
            CreatorId = 1,
            OpponentId = 2,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            Creator = creator,
            Opponent = opponent
        };
        
        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AcceptChallengeAsync(1, 2);

        // Assert
        Assert.Equal(ChallengeStatus.Active, result.Status);
    }

    [Fact]
    public async Task AcceptChallengeAsync_ShouldThrow_WhenWrongUser()
    {
        // Arrange
        var creator = new User { Id = 1, Name = "Creator", Email = "creator@test.com", GarminUserId = "garmin1" };
        var opponent = new User { Id = 2, Name = "Opponent", Email = "opponent@test.com", GarminUserId = "garmin2" };
        
        _context.Users.AddRange(creator, opponent);
        
        var challenge = new Challenge
        {
            Id = 1,
            Name = "Test Challenge",
            Type = ChallengeType.Distance,
            Status = ChallengeStatus.Pending,
            CreatorId = 1,
            OpponentId = 2,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            Creator = creator,
            Opponent = opponent
        };
        
        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.AcceptChallengeAsync(1, 1)); // Creator trying to accept own challenge
    }

    [Fact]
    public async Task GetChallengeProgressAsync_ShouldCalculateCorrectProgress()
    {
        // Arrange
        var creator = new User { Id = 1, Name = "Creator", Email = "creator@test.com", GarminUserId = "garmin1" };
        var opponent = new User { Id = 2, Name = "Opponent", Email = "opponent@test.com", GarminUserId = "garmin2" };
        
        _context.Users.AddRange(creator, opponent);
        
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(30);
        
        var challenge = new Challenge
        {
            Id = 1,
            Name = "Distance Challenge",
            Type = ChallengeType.Distance,
            Status = ChallengeStatus.Active,
            CreatorId = 1,
            OpponentId = 2,
            StartDate = startDate,
            EndDate = endDate,
            Creator = creator,
            Opponent = opponent
        };
        
        var activities = new[]
        {
            new Activity { UserId = 1, Distance = 50, ActivityType = "CYCLING", ActivityDate = startDate.AddDays(1) },
            new Activity { UserId = 1, Distance = 30, ActivityType = "CYCLING", ActivityDate = startDate.AddDays(2) },
            new Activity { UserId = 2, Distance = 40, ActivityType = "CYCLING", ActivityDate = startDate.AddDays(1) },
        };
        
        _context.Challenges.Add(challenge);
        _context.Activities.AddRange(activities);
        await _context.SaveChangesAsync();

        // Act
        var progress = await _service.GetChallengeProgressAsync(1);

        // Assert
        Assert.Equal(80, progress.CreatorProgress); // 50 + 30
        Assert.Equal(40, progress.OpponentProgress);
        Assert.Equal(2, progress.CreatorActivityCount);
        Assert.Equal(1, progress.OpponentActivityCount);
        Assert.Equal("Creator", progress.Winner);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}