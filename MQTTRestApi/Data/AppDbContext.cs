using Microsoft.EntityFrameworkCore;
using MQTTRestApi.Domain.Models;

namespace MQTTRestApi.Data;

/// <summary>
/// Kontekst za bazu podataka koji cuva poruke i pretplate
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<MqttMessage> Messages  { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Log> Logs { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<UserSubscription> UserSubscriptions { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<UserSubscription>()
            .HasIndex(s => new { s.UserId, s.TopicId })
            .IsUnique();
        
        modelBuilder.Entity<User>()
            .HasIndex(s => new { s.Id })
            .IsUnique();
        
        modelBuilder.Entity<Topic>()
            .HasIndex(s => new { s.Id })
            .IsUnique();
    }
}