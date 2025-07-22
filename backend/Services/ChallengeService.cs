// ABOUTME: Service for managing cycling challenges and calculating progress
// ABOUTME: Handles challenge creation, progress tracking, and winner determination
using Microsoft.EntityFrameworkCore;
using CyclingChallenge.Data;
using CyclingChallenge.Models;

namespace CyclingChallenge.Services;

public class ChallengeService
{
    private readonly ChallengeDbContext _context;

    public ChallengeService(ChallengeDbContext context)
    {
        _context = context;
    }

    public async Task<Challenge> CreateChallengeAsync(int creatorId, int opponentId, string name, ChallengeType type, double? targetValue = null)
    {
        var startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddSeconds(-1);

        var challenge = new Challenge
        {
            Name = name,
            Type = type,
            TargetValue = targetValue,
            StartDate = startDate,
            EndDate = endDate,
            CreatorId = creatorId,
            OpponentId = opponentId,
            Status = ChallengeStatus.Pending
        };

        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync();

        return await GetChallengeByIdAsync(challenge.Id);
    }

    public async Task<Challenge> AcceptChallengeAsync(int challengeId, int userId)
    {
        var challenge = await _context.Challenges
            .Include(c => c.Creator)
            .Include(c => c.Opponent)
            .FirstOrDefaultAsync(c => c.Id == challengeId);

        if (challenge == null)
            throw new InvalidOperationException("Challenge not found");

        if (challenge.OpponentId != userId)
            throw new InvalidOperationException("Only the challenged user can accept this challenge");

        if (challenge.Status != ChallengeStatus.Pending)
            throw new InvalidOperationException("Challenge is not in pending status");

        challenge.Status = ChallengeStatus.Active;
        await _context.SaveChangesAsync();

        return challenge;
    }

    public async Task<List<Challenge>> GetUserChallengesAsync(int userId)
    {
        return await _context.Challenges
            .Include(c => c.Creator)
            .Include(c => c.Opponent)
            .Where(c => c.CreatorId == userId || c.OpponentId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Challenge> GetChallengeByIdAsync(int challengeId)
    {
        var challenge = await _context.Challenges
            .Include(c => c.Creator)
            .Include(c => c.Opponent)
            .FirstOrDefaultAsync(c => c.Id == challengeId);

        if (challenge == null)
            throw new InvalidOperationException("Challenge not found");

        return challenge;
    }

    public async Task<ChallengeProgress> GetChallengeProgressAsync(int challengeId)
    {
        var challenge = await GetChallengeByIdAsync(challengeId);
        
        var creatorActivities = await GetUserActivitiesForPeriodAsync(challenge.CreatorId, challenge.StartDate, challenge.EndDate);
        var opponentActivities = await GetUserActivitiesForPeriodAsync(challenge.OpponentId, challenge.StartDate, challenge.EndDate);

        var creatorProgress = CalculateProgress(creatorActivities, challenge.Type);
        var opponentProgress = CalculateProgress(opponentActivities, challenge.Type);

        return new ChallengeProgress
        {
            Challenge = challenge,
            CreatorProgress = creatorProgress,
            OpponentProgress = opponentProgress,
            CreatorActivityCount = creatorActivities.Count,
            OpponentActivityCount = opponentActivities.Count
        };
    }

    private async Task<List<Activity>> GetUserActivitiesForPeriodAsync(int userId, DateTime startDate, DateTime endDate)
    {
        return await _context.Activities
            .Where(a => a.UserId == userId && 
                       a.ActivityDate >= startDate && 
                       a.ActivityDate <= endDate &&
                       IsCyclingActivity(a.ActivityType))
            .ToListAsync();
    }

    private static bool IsCyclingActivity(string activityType)
    {
        var cyclingTypes = new[]
        {
            "CYCLING", "MOUNTAIN_BIKING", "ROAD_BIKING", "INDOOR_CYCLING",
            "E_BIKE_FITNESS", "E_BIKE_MOUNTAIN", "GRAVEL_CYCLING",
            "CYCLOCROSS", "TRACK_CYCLING", "BMX"
        };
        
        return cyclingTypes.Contains(activityType.ToUpper());
    }

    private static double CalculateProgress(List<Activity> activities, ChallengeType challengeType)
    {
        if (!activities.Any()) return 0;

        return challengeType switch
        {
            ChallengeType.Distance => activities.Sum(a => a.Distance),
            ChallengeType.Climbing => activities.Sum(a => a.ElevationGain ?? 0),
            ChallengeType.AverageSpeed => activities.Where(a => a.AverageSpeed.HasValue).Average(a => a.AverageSpeed!.Value),
            _ => 0
        };
    }

    public async Task UpdateChallengeStatusesAsync()
    {
        var now = DateTime.UtcNow;
        var expiredChallenges = await _context.Challenges
            .Where(c => c.Status == ChallengeStatus.Active && c.EndDate < now)
            .ToListAsync();

        foreach (var challenge in expiredChallenges)
        {
            challenge.Status = ChallengeStatus.Completed;
        }

        if (expiredChallenges.Any())
        {
            await _context.SaveChangesAsync();
        }
    }
}

public class ChallengeProgress
{
    public Challenge Challenge { get; set; } = null!;
    public double CreatorProgress { get; set; }
    public double OpponentProgress { get; set; }
    public int CreatorActivityCount { get; set; }
    public int OpponentActivityCount { get; set; }
    
    public string Winner => CreatorProgress > OpponentProgress ? Challenge.Creator.Name : 
                           OpponentProgress > CreatorProgress ? Challenge.Opponent.Name : 
                           "Tie";
}