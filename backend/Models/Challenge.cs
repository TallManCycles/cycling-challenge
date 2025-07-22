// ABOUTME: Challenge entity model representing monthly cycling challenges between two users
// ABOUTME: Supports Distance, Climbing, and Speed challenge types with status tracking
using System.ComponentModel.DataAnnotations;

namespace CyclingChallenge.Models;

public enum ChallengeType
{
    Distance,
    Climbing,
    AverageSpeed
}

public enum ChallengeStatus
{
    Pending,
    Active,
    Completed,
    Cancelled
}

public class Challenge
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public ChallengeType Type { get; set; }
    
    public ChallengeStatus Status { get; set; } = ChallengeStatus.Pending;
    
    public double? TargetValue { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    public int CreatorId { get; set; }
    public virtual User Creator { get; set; } = null!;
    
    public int OpponentId { get; set; }
    public virtual User Opponent { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}