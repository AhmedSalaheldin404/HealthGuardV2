using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthGuard.Models;
using HealthGuard.Services;
using System.Security.Claims;

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
    public async Task<ActionResult<Patient>> CreatePatient(Patient patient)
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
    public async Task<ActionResult<Patient>> CreateMyPatientRecord(Patient patient)
    {
        try
        {
            // Ensure the UserId matches the logged-in user's ID
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            patient.UserId = userId; // Set the UserId instead of Id

            // Check if a patient record already exists for the logged-in user
            var existingPatient = await _patientService.GetPatientByUserId(userId);

            if (existingPatient == null)
            {
                // If no record exists, create a new one
                var result = await _patientService.CreatePatient(patient);
                return Ok(result);
            }
            else
            {
                // If a record exists, update it
                existingPatient.Name = patient.Name;
                existingPatient.DateOfBirth = patient.DateOfBirth;
                existingPatient.Gender = patient.Gender;
                existingPatient.MedicalHistory = patient.MedicalHistory;
                existingPatient.ContactNumber = patient.ContactNumber;
                existingPatient.Email = patient.Email;

                var result = await _patientService.UpdatePatient(existingPatient);
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            // Log the full exception details, including the inner exception
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            return StatusCode(500, "An error occurred while creating/updating the patient record.");
        }
    }

    // Only Admin, Doctor, and Patient can get a patient by ID
    [HttpGet("{id}")]
    public async Task<ActionResult<Patient>> GetPatient(int id)
    {
        var patient = await _patientService.GetPatient(id);

        if (patient == null)
            return NotFound();

        // If the user is a Patient, ensure they can only access their own record
        if (User.IsInRole("Patient"))
        {
            var patientId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            if (patient.Id != patientId)
                return Forbid(); // Deny access
        }

        // If the user is a Doctor, ensure they can only access their own patients
        if (User.IsInRole("Doctor"))
        {
            var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            if (patient.DoctorId != doctorId)
                return Forbid(); // Deny access
        }

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
    public async Task<ActionResult<Patient>> UpdatePatient(int id, Patient patient)
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