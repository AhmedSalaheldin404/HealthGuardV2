using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthGuard.Models;
using HealthGuard.Services;
using System.Threading.Tasks;

namespace HealthAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosisController : ControllerBase
    {
        private readonly IDiagnosticService _diagnosticService;
        private readonly IMLModelService _mlModelService;
        private readonly INotificationService _notificationService;

        public DiagnosisController(
            IDiagnosticService diagnosticService,
            IMLModelService mlModelService,
            INotificationService notificationService)
        {
            _diagnosticService = diagnosticService;
            _mlModelService = mlModelService;
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<ActionResult<Diagnosis>> CreateDiagnosis(DiagnosisRequest request)
        {
            try
            {
                var (prediction, confidence) = await _mlModelService.Predict(request.Features, request.ModelType);

                var diagnosis = new Diagnosis
                {
                    PatientId = request.PatientId,
                    DiagnosisDate = DateTime.UtcNow,
                    Prediction = prediction,
                    Confidence = confidence,
                    Features = request.Features,
                    Status = confidence < 0.8 ? DiagnosisStatus.RequiresReview : DiagnosisStatus.Completed
                };

                var result = await _diagnosticService.CreateDiagnosis(diagnosis);

                if (diagnosis.Status == DiagnosisStatus.RequiresReview)
                {
                    await _notificationService.SendDoctorAlert(
                        request.DoctorEmail,
                        request.PatientId,
                        "High"
                    );
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Diagnosis>> GetDiagnosis(int id)
        {
            var diagnosis = await _diagnosticService.GetDiagnosis(id);
            if (diagnosis == null)
                return NotFound();

            return Ok(diagnosis);
        }
    }
}