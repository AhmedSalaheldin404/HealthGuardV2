using System.Collections.Generic;

namespace HealthGuard.Models
{
    public class DiagnosisRequest
    {
        public int PatientId { get; set; }
        public Dictionary<string, double> Features { get; set; } = new();
        public string DoctorEmail { get; set; } = string.Empty;
        public ModelType ModelType { get; set; }
    }
}