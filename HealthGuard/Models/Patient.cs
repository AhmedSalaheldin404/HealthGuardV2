using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;
using HealthGuard.Models.HealthGuard.Models;

namespace HealthGuard.Models
{
    public class Patient
    {
        [SwaggerSchema(ReadOnly = true)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } = string.Empty;

        public string? MedicalHistory { get; set; }

        [Required]
        public string ContactNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public int? DoctorId { get; set; } // Add this field to associate a patient with a doctor
        public int UserId { get; set; }

        [JsonIgnore]
        public List<Diagnosis>? Diagnoses { get; set; } = new();
    }
}