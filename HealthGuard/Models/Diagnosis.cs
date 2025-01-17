using System;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.Annotations;

namespace HealthGuard.Models
{
    namespace HealthGuard.Models
    {
     
            public class Diagnosis
            {
                public int Id { get; set; }
                public int PatientId { get; set; }
                public Patient Patient { get; set; } = null!;
                public DateTime DiagnosisDate { get; set; }
                public string Diagnose { get; set; } = string.Empty; // Renamed from "Prediction" to "Diagnose"
                public double Confidence { get; set; }
                public Dictionary<string, double> Features { get; set; } = new();
                public string? DoctorNotes { get; set; }
                public DiagnosisStatus Status { get; set; }
            }
        
    }

    public enum DiagnosisStatus
    {
        Pending,
        Completed,
        RequiresReview
    }
}