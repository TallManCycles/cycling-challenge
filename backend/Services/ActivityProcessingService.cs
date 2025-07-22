// ABOUTME: Service for processing Garmin activity data and updating challenges
// ABOUTME: Handles activity fetching from Garmin API and challenge progress updates
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CyclingChallenge.Data;
using CyclingChallenge.Models;

namespace CyclingChallenge.Services;

public class ActivityProcessingService
{
    private readonly HttpClient _httpClient;
    private readonly ChallengeDbContext _context;
    private readonly ILogger<ActivityProcessingService> _logger;

    public ActivityProcessingService(HttpClient httpClient, ChallengeDbContext context, ILogger<ActivityProcessingService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
    }

    public async Task ProcessActivityPingAsync(string callbackUrl, string accessToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.GetAsync(callbackUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch activity data from {callbackUrl}: {response.StatusCode}");
                return;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var activities = JsonSerializer.Deserialize<JsonElement>(responseBody);

            if (activities.TryGetProperty("activities", out var activitiesArray))
            {
                foreach (var activityElement in activitiesArray.EnumerateArray())
                {
                    await ProcessSingleActivityAsync(activityElement, accessToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing activity ping from {callbackUrl}");
            throw;
        }
    }

    private async Task ProcessSingleActivityAsync(JsonElement activityData, string accessToken)
    {
        try
        {
            var activityId = activityData.GetProperty("activityId").GetString();
            var activityType = activityData.GetProperty("activityType").GetString();
            var startTimeLocal = activityData.GetProperty("startTimeLocal").GetString();

            if (string.IsNullOrEmpty(activityId) || string.IsNullOrEmpty(activityType) || string.IsNullOrEmpty(startTimeLocal))
            {
                _logger.LogWarning("Incomplete activity data received");
                return;
            }

            if (!IsCyclingActivity(activityType))
            {
                _logger.LogDebug($"Skipping non-cycling activity: {activityType}");
                return;
            }

            var existingActivity = await _context.Activities
                .FirstOrDefaultAsync(a => a.GarminActivityId == activityId);

            if (existingActivity != null)
            {
                _logger.LogDebug($"Activity {activityId} already exists, skipping");
                return;
            }

            var user = await GetUserByAccessTokenAsync(accessToken);
            if (user == null)
            {
                _logger.LogError("Could not find user for access token");
                return;
            }

            var activityDetails = await FetchActivityDetailsAsync(activityId, accessToken);
            if (activityDetails == null)
            {
                _logger.LogError($"Could not fetch details for activity {activityId}");
                return;
            }

            var activity = new Activity
            {
                GarminActivityId = activityId,
                ActivityType = activityType,
                Distance = GetDoubleValue(activityDetails.Value, "distance") / 1000.0, // Convert meters to km
                ElevationGain = GetDoubleValue(activityDetails.Value, "elevationGain"),
                AverageSpeed = GetDoubleValue(activityDetails.Value, "averageSpeed") * 3.6, // Convert m/s to km/h
                ActivityDate = DateTime.Parse(startTimeLocal),
                UserId = user.Id
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            await AssignActivityToChallengesAsync(activity);

            _logger.LogInformation($"Successfully processed activity {activityId} for user {user.Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing single activity");
        }
    }

    private async Task<JsonElement?> FetchActivityDetailsAsync(string activityId, string accessToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.GetAsync($"https://apis.garmin.com/wellness-api/rest/activities/{activityId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch activity details for {activityId}: {response.StatusCode}");
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JsonElement>(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching activity details for {activityId}");
            return null;
        }
    }

    private async Task<User?> GetUserByAccessTokenAsync(string accessToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.GarminAccessToken == accessToken);
    }

    private async Task AssignActivityToChallengesAsync(Activity activity)
    {
        var activeChallenges = await _context.Challenges
            .Where(c => (c.CreatorId == activity.UserId || c.OpponentId == activity.UserId) &&
                       c.Status == ChallengeStatus.Active &&
                       activity.ActivityDate >= c.StartDate &&
                       activity.ActivityDate <= c.EndDate)
            .ToListAsync();

        foreach (var challenge in activeChallenges)
        {
            var challengeActivity = new Activity
            {
                GarminActivityId = activity.GarminActivityId + "_" + challenge.Id,
                ActivityType = activity.ActivityType,
                Distance = activity.Distance,
                ElevationGain = activity.ElevationGain,
                AverageSpeed = activity.AverageSpeed,
                ActivityDate = activity.ActivityDate,
                UserId = activity.UserId,
                ChallengeId = challenge.Id
            };

            _context.Activities.Add(challengeActivity);
        }

        if (activeChallenges.Any())
        {
            await _context.SaveChangesAsync();
        }
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

    private static double GetDoubleValue(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetDouble();
        }
        return 0.0;
    }
}