using Microsoft.EntityFrameworkCore;
using HealthGuard.Models;
using System.Text.Json;


namespace HealthGuard.Data;

public class HealthDbContext : DbContext
{
    public HealthDbContext(DbContextOptions<HealthDbContext> options)
        : base(options)
    {
    }

    
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Diagnosis> Diagnoses { get; set; }
    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>()
            .HasMany(p => p.Diagnoses)
            .WithOne(d => d.Patient)
            .HasForeignKey(d => d.PatientId);
            
        modelBuilder.Entity<Diagnosis>()
            .Property(d => d.Features)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(v, (JsonSerializerOptions)null!)!);
                
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
            
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}