using System.Collections.Generic;

namespace HealthGuard.Models
{
    public class DiagnosisRequest
    {
        public int PatientId { get; set; }
        public IFormFile ImageFile { get; set; } // Add this field for image upload
     //   public ModelType ModelType { get; set; }
    }
   
    
}