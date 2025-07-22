// ABOUTME: Entity Framework DbContext for the cycling challenge database
// ABOUTME: Configures relationships between Users, Challenges, and Activities
using Microsoft.EntityFrameworkCore;
using CyclingChallenge.Models;

namespace CyclingChallenge.Data;

public class ChallengeDbContext : DbContext
{
    public ChallengeDbContext(DbContextOptions<ChallengeDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Challenge> Challenges { get; set; } = null!;
    public DbSet<Activity> Activities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.GarminUserId).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            
            entity.HasMany(u => u.CreatedChallenges)
                  .WithOne(c => c.Creator)
                  .HasForeignKey(c => c.CreatorId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasMany(u => u.OpponentChallenges)
                  .WithOne(c => c.Opponent)
                  .HasForeignKey(c => c.OpponentId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasMany(u => u.Activities)
                  .WithOne(a => a.User)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Challenge>(entity =>
        {
            entity.HasKey(c => c.Id);
            
            entity.HasMany(c => c.Activities)
                  .WithOne(a => a.Challenge)
                  .HasForeignKey(a => a.ChallengeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.GarminActivityId).IsUnique();
            entity.HasIndex(a => new { a.UserId, a.ActivityDate });
        });
    }
}