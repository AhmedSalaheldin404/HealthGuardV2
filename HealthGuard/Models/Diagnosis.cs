using System;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.Annotations;

namespace HealthGuard.Models
{
    public class Diagnosis
    {
        [SwaggerSchema(ReadOnly = true)]  // This field will be ignored in Swagger
        public int Id { get; set; }

        [SwaggerSchema(ReadOnly = true)]  // This field will be ignored in Swagger
        public int PatientId { get; set; }

        public Patient Patient { get; set; } = null!;

        public DateTime DiagnosisDate { get; set; }

        public string Prediction { get; set; } = string.Empty;

        public double Confidence { get; set; }

        public Dictionary<string, double> Features { get; set; } = new();

        public string? DoctorNotes { get; set; }

        public DiagnosisStatus Status { get; set; }
    }

    public enum DiagnosisStatus
    {
        Pending,
        Completed,
        RequiresReview
    }
}