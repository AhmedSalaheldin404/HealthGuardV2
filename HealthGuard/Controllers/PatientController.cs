using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthGuard.Models;
using HealthGuard.Services;
using System.Security.Claims;
using HealthGuard.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    // Only Admin and Doctor can create patients
    [HttpPost]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<ActionResult<Patient>> CreatePatient([FromForm] Patient patient)
    {
        try
        {
            // If the user is a Doctor, ensure the patient is associated with the doctor
            if (User.IsInRole("Doctor"))
            {
                var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                patient.DoctorId = doctorId; // Assuming Patient has a DoctorId field
            }

            var result = await _patientService.CreatePatient(patient);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "Patient")]
    [HttpPost("create-my-record")]
    public async Task<ActionResult<PatientResponse>> CreateMyPatientRecord([FromForm] CreatePatientRequest request)
    {
        try
        {
            // Ensure the UserId matches the logged-in user's ID
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // Create a new Patient object from the request
            var patient = new Patient
            {
                UserId = userId, // Set the UserId
                Name = request.Name,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                MedicalHistory = request.MedicalHistory,
                ContactNumber = request.ContactNumber,
                Email = request.Email,
                DoctorId = null // Ensure DoctorId is null
            };

            // Check if a patient record already exists for the logged-in user
            var existingPatient = await _patientService.GetPatientByUserId(userId);

            if (existingPatient == null)
            {
                // If no record exists, create a new one
                var result = await _patientService.CreatePatient(patient);

                // Return a PatientResponse (without DoctorId)
                var response = new PatientResponse
                {
                    Id = result.Id,
                    Name = result.Name,
                    DateOfBirth = result.DateOfBirth,
                    Gender = result.Gender,
                    MedicalHistory = result.MedicalHistory,
                    ContactNumber = result.ContactNumber,
                    Email = result.Email,
                    UserId = result.UserId
                };

                return Ok(response);
            }
            else
            {
                // If a record exists, update it
                existingPatient.Name = request.Name;
                existingPatient.DateOfBirth = request.DateOfBirth;
                existingPatient.Gender = request.Gender;
                existingPatient.MedicalHistory = request.MedicalHistory;
                existingPatient.ContactNumber = request.ContactNumber;
                existingPatient.Email = request.Email;

                var result = await _patientService.UpdatePatient(existingPatient);

                // Return a PatientResponse (without DoctorId)
                var response = new PatientResponse
                {
                    Id = result.Id,
                    Name = result.Name,
                    DateOfBirth = result.DateOfBirth,
                    Gender = result.Gender,
                    MedicalHistory = result.MedicalHistory,
                    ContactNumber = result.ContactNumber,
                    Email = result.Email,
                    UserId = result.UserId
                };

                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            // Log the full exception details, including the inner exception
            return StatusCode(500, "An error occurred while creating/updating the patient record.");
        }
    }

    [Authorize(Roles = "Admin,Doctor")]
    [HttpPost("assign-patient-to-doctor")]
    public async Task<IActionResult> AssignPatientToDoctor(
        [FromForm] int patientId,
        [FromForm] int doctorId,
        [FromServices] HealthDbContext context, // Inject HealthDbContext directly
        [FromServices] ILogger<PatientController> logger) // Inject ILogger directly
    {
        try
        {
            // Check if the patient exists
            var patient = await context.Patients
                .Include(p => p.Diagnoses) // Include related data if needed
                .FirstOrDefaultAsync(p => p.Id == patientId);
            if (patient == null)
                return NotFound("Patient not found.");

            // Check if the doctor exists and has the Doctor role
            var doctor = await context.Users.FirstOrDefaultAsync(u => u.Id == doctorId && u.Role == UserRole.Doctor);
            if (doctor == null)
                return NotFound("Doctor not found.");

            // Prevent self-assignment
            var loggedInUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            if (doctorId == loggedInUserId)
            {
                return BadRequest("A doctor cannot assign a patient to themselves.");
            }

            // Assign the patient to the doctor
            patient.DoctorId = doctorId;
            await context.SaveChangesAsync();

            // Log the assignment
            logger.LogInformation($"Patient {patientId} assigned to Doctor {doctorId} by User {loggedInUserId}.");

            // Return the updated patient record (including the userId)
            return Ok(patient);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while assigning a patient to a doctor.");
            return StatusCode(500, ex.Message);
        }
    }

    // Only Admin, Doctor, and Patient can get a patient by ID
    [HttpGet("{id}")]
    public async Task<ActionResult<Patient>> GetPatient(int id)
    {
        // Log the request for debugging
        Console.WriteLine($"GetPatient request received for ID: {id}");
        Console.WriteLine($"Logged-in User ID: {User.FindFirst(ClaimTypes.NameIdentifier)?.Value}");
        Console.WriteLine($"Logged-in User Role: {User.FindFirst(ClaimTypes.Role)?.Value}");

        // Retrieve the patient from the database
        var patient = await _patientService.GetPatient(id);

        if (patient == null)
        {
            Console.WriteLine($"Patient with ID {id} not found.");
            return NotFound();
        }

        // Log the patient details for debugging
        Console.WriteLine($"Patient ID: {patient.Id}, UserId: {patient.UserId}, DoctorId: {patient.DoctorId}");

        // If the user is a Patient, ensure they can only access their own record
        if (User.IsInRole("Patient"))
        {
            var loggedInUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            if (patient.UserId != loggedInUserId)
            {
                Console.WriteLine($"Access denied: Patient ID {patient.Id} does not belong to logged-in user ID {loggedInUserId}.");
                return Forbid(); // Deny access
            }
        }

        // If the user is a Doctor, ensure they can only access their own patients
        if (User.IsInRole("Doctor"))
        {
            var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            if (patient.DoctorId != doctorId)
            {
                Console.WriteLine($"Access denied: Patient ID {patient.Id} is not assigned to logged-in doctor ID {doctorId}.");
                return Forbid(); // Deny access
            }
        }

        // Return the patient record
        return Ok(patient);
    }

    // Only Admin can get all patients
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<Patient>>> GetAllPatients()
    {
        var patients = await _patientService.GetAllPatients();
        return Ok(patients);
    }

    // Only Admin and Doctor can update patients
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<ActionResult<Patient>> UpdatePatient(int id, [FromForm] Patient patient)
    {
        // If the user is a Doctor, ensure they can only update their own patients
        if (User.IsInRole("Doctor"))
        {
            var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var existingPatient = await _patientService.GetPatient(id);
            if (existingPatient == null || existingPatient.DoctorId != doctorId)
                return Forbid(); // Deny access
        }

        patient.Id = id;

        try
        {
            var updatedPatient = await _patientService.UpdatePatient(patient);
            if (updatedPatient == null)
                return NotFound($"Patient with ID {id} not found.");

            return Ok(updatedPatient);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while updating the patient.", details = ex.Message });
        }
    }

    // Only Admin and Doctor can delete patients
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> DeletePatient(int id)
    {
        // If the user is a Doctor, ensure they can only delete their own patients
        if (User.IsInRole("Doctor"))
        {
            var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var existingPatient = await _patientService.GetPatient(id);
            if (existingPatient == null || existingPatient.DoctorId != doctorId)
                return Forbid(); // Deny access
        }

        try
        {
            await _patientService.DeletePatient(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}