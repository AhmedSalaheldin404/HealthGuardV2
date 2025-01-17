using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthGuard.Models;
using HealthGuard.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using HealthGuard.Models.HealthGuard.Models;

namespace HealthAPI.Controllers
{
    [Authorize] // Require authentication for all endpoints in this controller
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosisController : ControllerBase
    {
        private readonly IMLModelService _mlModelService;
        private readonly IDiagnosticService _diagnosticService;
        private readonly IPatientService _patientService;

        public DiagnosisController(
            IMLModelService mlModelService,
            IDiagnosticService diagnosticService,
            IPatientService patientService)
        {
            _mlModelService = mlModelService;
            _diagnosticService = diagnosticService;
            _patientService = patientService;
        }

        [HttpPost("diagnose")]
        public async Task<ActionResult<Diagnosis>> Diagnose([FromForm] DiagnosisRequest request)
        {
            try
            {
                // Use the MLModelService to predict the diagnosis
                var (diagnose, confidence) = await _mlModelService.PredictFromImage(request.ImageFile);

                // Create a new diagnosis object
                var diagnosis = new Diagnosis
                {
                    PatientId = request.PatientId,
                    DiagnosisDate = DateTime.UtcNow,
                    Diagnose = diagnose,
                    Confidence = confidence,
                    Features = new Dictionary<string, double>(),
                    DoctorNotes = null,
                    Status = confidence < 0.8 ? DiagnosisStatus.RequiresReview : DiagnosisStatus.Completed
                };

                // Save the diagnosis to the database
                var savedDiagnosis = await _diagnosticService.CreateDiagnosis(diagnosis);

                return Ok(savedDiagnosis);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")] // Endpoint to get a diagnosis by ID
        public async Task<ActionResult<Diagnosis>> GetDiagnosis(int id)
        {
            try
            {
                // Get the current user's ID and role from the JWT token
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Retrieve the diagnosis
                var diagnosis = await _diagnosticService.GetDiagnosis(id);

                if (diagnosis == null)
                {
                    return NotFound($"Diagnosis with ID {id} not found.");
                }

                // Check if the current user is the patient or the assigned doctor
                var patient = await _patientService.GetPatient(diagnosis.PatientId);
                if (patient == null)
                {
                    return NotFound($"Patient with ID {diagnosis.PatientId} not found.");
                }

                if (userRole == "Patient" && patient.UserId != userId)
                {
                    return Forbid(); // Deny access if the user is not the patient
                }

                if (userRole == "Doctor" && patient.DoctorId != userId)
                {
                    return Forbid(); // Deny access if the user is not the assigned doctor
                }

                return Ok(diagnosis);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}