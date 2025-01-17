using HealthGuard.Models;
using HealthGuard.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using HealthGuard.Models.HealthGuard.Models;

namespace HealthGuard.Services
{
    public interface IDiagnosticService
    {
        Task<Diagnosis> CreateDiagnosis(Diagnosis diagnosis);
        Task<Diagnosis?> GetDiagnosis(int id);
        Task<List<Diagnosis>> GetPatientDiagnoses(int patientId);
    }

    public class DiagnosticService : IDiagnosticService
    {
        private readonly HealthDbContext _context;

        public DiagnosticService(HealthDbContext context)
        {
            _context = context;
        }

        public async Task<Diagnosis> CreateDiagnosis(Diagnosis diagnosis)
        {
            _context.Diagnoses.Add(diagnosis);
            await _context.SaveChangesAsync();
            return diagnosis;
        }

        public async Task<Diagnosis?> GetDiagnosis(int id)
        {
            return await _context.Diagnoses
                .Include(d => d.Patient) // Include the related Patient entity
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<List<Diagnosis>> GetPatientDiagnoses(int patientId)
        {
            return await _context.Diagnoses
                .Where(d => d.PatientId == patientId)
                .Include(d => d.Patient) // Include the related Patient entity
                .ToListAsync();
        }
    }
}