// ABOUTME: User entity model for storing Garmin-authenticated users
// ABOUTME: Includes Garmin OAuth tokens and user profile information
using System.ComponentModel.DataAnnotations;

namespace CyclingChallenge.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string GarminUserId { get; set; } = string.Empty;
    
    [Required]
    public string GarminAccessToken { get; set; } = string.Empty;
    
    [Required]
    public string GarminRefreshToken { get; set; } = string.Empty;
    
    public DateTime? TokenExpiry { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<Challenge> CreatedChallenges { get; set; } = new List<Challenge>();
    public virtual ICollection<Challenge> OpponentChallenges { get; set; } = new List<Challenge>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}