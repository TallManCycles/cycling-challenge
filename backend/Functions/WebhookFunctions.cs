// ABOUTME: Azure Functions for Garmin webhook endpoints with 30-second response compliance
// ABOUTME: Handles activity notifications, deregistration, and permission changes
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using CyclingChallenge.Services;
using CyclingChallenge.Data;

namespace CyclingChallenge.Functions;

public class WebhookFunctions
{
    private readonly ILogger<WebhookFunctions> _logger;
    private readonly ActivityProcessingService _activityService;
    private readonly ChallengeDbContext _context;

    public WebhookFunctions(ILogger<WebhookFunctions> logger, ActivityProcessingService activityService, ChallengeDbContext context)
    {
        _logger = logger;
        _activityService = activityService;
        _context = context;
    }

    [Function("ActivityWebhook")]
    public async Task<HttpResponseData> ActivityWebhook([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhooks/activity")] HttpRequestData req)
    {
        _logger.LogInformation("Received activity webhook");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var pingData = JsonSerializer.Deserialize<JsonElement>(requestBody ?? "{}");

            if (pingData.TryGetProperty("ping", out var ping))
            {
                var callbackUrl = ping.GetProperty("callbackURL").GetString();
                var userId = ping.GetProperty("userId").GetString();

                if (string.IsNullOrEmpty(callbackUrl) || string.IsNullOrEmpty(userId))
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Missing required ping data");
                    return badResponse;
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.GarminUserId == userId);

                if (user == null)
                {
                    _logger.LogWarning($"Received ping for unknown user: {userId}");
                    var response = req.CreateResponse(HttpStatusCode.OK);
                    return response;
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _activityService.ProcessActivityPingAsync(callbackUrl, user.GarminAccessToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing activity ping for user {userId}");
                    }
                });

                var successResponse = req.CreateResponse(HttpStatusCode.OK);
                return successResponse;
            }

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            return okResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing activity webhook");
            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }

    [Function("DeregistrationWebhook")]
    public async Task<HttpResponseData> DeregistrationWebhook([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhooks/deregistration")] HttpRequestData req)
    {
        _logger.LogInformation("Received deregistration webhook");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var deregData = JsonSerializer.Deserialize<JsonElement>(requestBody ?? "{}");

            if (deregData.TryGetProperty("deregistrations", out var deregistrations))
            {
                foreach (var dereg in deregistrations.EnumerateArray())
                {
                    var userId = dereg.GetProperty("userId").GetString();
                    var deregDate = dereg.GetProperty("deregistrationTimeStamp").GetString();

                    if (!string.IsNullOrEmpty(userId))
                    {
                        var user = await _context.Users
                            .FirstOrDefaultAsync(u => u.GarminUserId == userId);

                        if (user != null)
                        {
                            _logger.LogInformation($"User {userId} deregistered at {deregDate}");
                            
                            user.GarminAccessToken = string.Empty;
                            user.GarminRefreshToken = string.Empty;
                            user.TokenExpiry = DateTime.UtcNow.AddDays(-1);
                            
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing deregistration webhook");
            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }

    [Function("UserPermissionWebhook")]
    public async Task<HttpResponseData> UserPermissionWebhook([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhooks/permissions")] HttpRequestData req)
    {
        _logger.LogInformation("Received user permission webhook");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var permData = JsonSerializer.Deserialize<JsonElement>(requestBody ?? "{}");

            if (permData.TryGetProperty("userPermissions", out var permissions))
            {
                foreach (var perm in permissions.EnumerateArray())
                {
                    var userId = perm.GetProperty("userId").GetString();
                    var permissionType = perm.GetProperty("userPermission").GetString();

                    _logger.LogInformation($"User {userId} permission changed to: {permissionType}");

                    if (permissionType == "NO_PERMISSION" && !string.IsNullOrEmpty(userId))
                    {
                        var user = await _context.Users
                            .FirstOrDefaultAsync(u => u.GarminUserId == userId);

                        if (user != null)
                        {
                            user.GarminAccessToken = string.Empty;
                            user.GarminRefreshToken = string.Empty;
                            user.TokenExpiry = DateTime.UtcNow.AddDays(-1);
                            
                            await _context.SaveChangesAsync();
                            
                            _logger.LogInformation($"Cleared tokens for user {userId} due to NO_PERMISSION");
                        }
                    }
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user permission webhook");
            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}