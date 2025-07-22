// ABOUTME: Azure Functions for challenge management operations
// ABOUTME: Handles challenge creation, acceptance, listing, and progress tracking
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using CyclingChallenge.Services;
using CyclingChallenge.Models;

namespace CyclingChallenge.Functions;

public class ChallengeFunctions
{
    private readonly ILogger<ChallengeFunctions> _logger;
    private readonly ChallengeService _challengeService;

    public ChallengeFunctions(ILogger<ChallengeFunctions> logger, ChallengeService challengeService)
    {
        _logger = logger;
        _challengeService = challengeService;
    }

    [Function("CreateChallenge")]
    public async Task<HttpResponseData> CreateChallenge([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "challenges")] HttpRequestData req)
    {
        _logger.LogInformation("Creating new challenge");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            var challengeData = JsonSerializer.Deserialize<JsonElement>(requestBody ?? "{}");

            var creatorId = challengeData.GetProperty("creatorId").GetInt32();
            var opponentId = challengeData.GetProperty("opponentId").GetInt32();
            var name = challengeData.GetProperty("name").GetString();
            var typeString = challengeData.GetProperty("type").GetString();
            
            if (!Enum.TryParse<ChallengeType>(typeString, true, out var type))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid challenge type");
                return badResponse;
            }

            double? targetValue = null;
            if (challengeData.TryGetProperty("targetValue", out var targetValueElement) && targetValueElement.ValueKind != JsonValueKind.Null)
            {
                targetValue = targetValueElement.GetDouble();
            }

            if (string.IsNullOrEmpty(name))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Challenge name is required");
                return badResponse;
            }

            var challenge = await _challengeService.CreateChallengeAsync(creatorId, opponentId, name, type, targetValue);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new
            {
                id = challenge.Id,
                name = challenge.Name,
                type = challenge.Type.ToString(),
                status = challenge.Status.ToString(),
                targetValue = challenge.TargetValue,
                startDate = challenge.StartDate,
                endDate = challenge.EndDate,
                creator = new { id = challenge.Creator.Id, name = challenge.Creator.Name },
                opponent = new { id = challenge.Opponent.Id, name = challenge.Opponent.Name },
                createdAt = challenge.CreatedAt
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating challenge");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("AcceptChallenge")]
    public async Task<HttpResponseData> AcceptChallenge([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "challenges/{challengeId}/accept")] HttpRequestData req)
    {
        _logger.LogInformation("Accepting challenge");

        try
        {
            var challengeIdStr = req.FunctionContext.BindingContext.BindingData["challengeId"]?.ToString();
            if (!int.TryParse(challengeIdStr, out var challengeId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid challenge ID");
                return badResponse;
            }

            var requestBody = await req.ReadAsStringAsync();
            var acceptData = JsonSerializer.Deserialize<JsonElement>(requestBody ?? "{}");
            var userId = acceptData.GetProperty("userId").GetInt32();

            var challenge = await _challengeService.AcceptChallengeAsync(challengeId, userId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                id = challenge.Id,
                name = challenge.Name,
                type = challenge.Type.ToString(),
                status = challenge.Status.ToString(),
                targetValue = challenge.TargetValue,
                startDate = challenge.StartDate,
                endDate = challenge.EndDate,
                creator = new { id = challenge.Creator.Id, name = challenge.Creator.Name },
                opponent = new { id = challenge.Opponent.Id, name = challenge.Opponent.Name }
            });

            return response;
        }
        catch (InvalidOperationException ex)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteStringAsync(ex.Message);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting challenge");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("GetChallenges")]
    public async Task<HttpResponseData> GetChallenges([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "challenges")] HttpRequestData req)
    {
        _logger.LogInformation("Getting user challenges");

        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var userIdStr = query["userId"];
            
            if (!int.TryParse(userIdStr, out var userId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("User ID is required");
                return badResponse;
            }

            var challenges = await _challengeService.GetUserChallengesAsync(userId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(challenges.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                type = c.Type.ToString(),
                status = c.Status.ToString(),
                targetValue = c.TargetValue,
                startDate = c.StartDate,
                endDate = c.EndDate,
                creator = new { id = c.Creator.Id, name = c.Creator.Name },
                opponent = new { id = c.Opponent.Id, name = c.Opponent.Name },
                createdAt = c.CreatedAt
            }));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting challenges");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }

    [Function("GetChallengeProgress")]
    public async Task<HttpResponseData> GetChallengeProgress([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "challenges/{challengeId}/progress")] HttpRequestData req)
    {
        _logger.LogInformation("Getting challenge progress");

        try
        {
            var challengeIdStr = req.FunctionContext.BindingContext.BindingData["challengeId"]?.ToString();
            if (!int.TryParse(challengeIdStr, out var challengeId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid challenge ID");
                return badResponse;
            }

            var progress = await _challengeService.GetChallengeProgressAsync(challengeId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                challenge = new
                {
                    id = progress.Challenge.Id,
                    name = progress.Challenge.Name,
                    type = progress.Challenge.Type.ToString(),
                    status = progress.Challenge.Status.ToString(),
                    targetValue = progress.Challenge.TargetValue,
                    startDate = progress.Challenge.StartDate,
                    endDate = progress.Challenge.EndDate
                },
                creator = new
                {
                    id = progress.Challenge.Creator.Id,
                    name = progress.Challenge.Creator.Name,
                    progress = progress.CreatorProgress,
                    activityCount = progress.CreatorActivityCount
                },
                opponent = new
                {
                    id = progress.Challenge.Opponent.Id,
                    name = progress.Challenge.Opponent.Name,
                    progress = progress.OpponentProgress,
                    activityCount = progress.OpponentActivityCount
                },
                winner = progress.Winner
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting challenge progress");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}