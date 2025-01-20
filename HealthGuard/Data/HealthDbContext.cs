using Microsoft.EntityFrameworkCore;
using HealthGuard.Models;
using System.Text.Json;
using System.Collections.Generic;
using HealthGuard.Models.HealthGuard.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
        // Specify the table name for the User entity
        modelBuilder.Entity<User>().ToTable("Users"); // Ensure this matches the actual table name in the database

        // Configure the relationship between Patient and Diagnosis
        modelBuilder.Entity<Patient>()
            .HasMany(p => p.Diagnoses)
            .WithOne(d => d.Patient)
            .HasForeignKey(d => d.PatientId);

        // Configure the Features property in Diagnosis to store as JSON
        modelBuilder.Entity<Diagnosis>()
            .Property(d => d.Features)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, (JsonSerializerOptions)null!)!,
                // Add a value comparer for the dictionary
                new ValueComparer<Dictionary<string, double>>(
                    (c1, c2) => c1!.SequenceEqual(c2!), // Compare two dictionaries
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())), // Generate hash code
                    c => c.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) // Create a snapshot of the dictionary
                )
            );

        // Ensure Email is unique in the User entity
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}