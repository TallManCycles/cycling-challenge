// ABOUTME: Activity entity model for storing cycling activity data from Garmin
// ABOUTME: Contains distance, elevation, and speed metrics for challenge calculations
using System.ComponentModel.DataAnnotations;

namespace CyclingChallenge.Models;

public class Activity
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string GarminActivityId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ActivityType { get; set; } = string.Empty;
    
    public double Distance { get; set; }
    
    public double? ElevationGain { get; set; }
    
    public double? AverageSpeed { get; set; }
    
    public DateTime ActivityDate { get; set; }
    
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
    
    public int? ChallengeId { get; set; }
    public virtual Challenge? Challenge { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}