using Habitarium.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Habitarium.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Material> Materials => Set<Material>();
    public DbSet<AppUser> AppUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var specsComparer = new ValueComparer<Dictionary<string, string>>(
        (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null)
               == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
        v => JsonSerializer.Deserialize<Dictionary<string, string>>(
                 JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                 (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());

        modelBuilder.Entity<Material>()
            .Property(m => m.Specs)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v)
                        ? new Dictionary<string, string>()
                        : JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)
                            ?? new Dictionary<string, string>(),
                specsComparer);

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}