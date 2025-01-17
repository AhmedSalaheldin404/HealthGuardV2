using HealthGuard.Models;
using HealthGuard.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthGuard.Services
{
    public interface IPatientService
    {
        Task<Patient> CreatePatient(Patient patient);
        Task<Patient?> GetPatient(int id);
        Task<List<Patient>> GetAllPatients();
        Task<Patient> UpdatePatient(Patient patient);
        Task DeletePatient(int id); // New method to delete a patient
        Task<Patient?> GetPatientByUserId(int userId);
    }
    public class PatientService : IPatientService
    {
        private readonly HealthDbContext _context;

        public PatientService(HealthDbContext context)
        {
            _context = context;
        }

        public async Task<Patient> CreatePatient(Patient patient)
        {
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
            return patient;
        }

        public async Task<Patient?> GetPatient(int id)
        {
            return await _context.Patients
                .Include(p => p.Diagnoses)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Patient>> GetAllPatients()
        {
            return await _context.Patients
                .Include(p => p.Diagnoses)
                .ToListAsync();
        }

        public async Task<List<Patient>> GetPatientsByDoctorId(int doctorId)
        {
            return await _context.Patients
                .Where(p => p.DoctorId == doctorId)
                .Include(p => p.Diagnoses)
                .ToListAsync();
        }

        public async Task<Patient> UpdatePatient(Patient patient)
        {
            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();
            return patient;
        }
        public async Task<Patient?> GetPatientByUserId(int userId)
        {
            return await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }
        public async Task DeletePatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();
            }
        }

    }
}