using Microsoft.EntityFrameworkCore;
using MQTTRestApi.Domain.Models;

namespace MQTTRestApi.Data;

/// <summary>
/// Kontekst za bazu podataka koji cuva poruke i pretplate
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<MqttMessage> Messages  { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<User> Users { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
}