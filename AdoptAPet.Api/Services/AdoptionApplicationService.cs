using AdoptAPet.Api.Data;
using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Interfaces;
using AdoptAPet.Api.Models;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Threading.Channels;

namespace AdoptAPet.Api.Services
{
    public class AdoptionApplicationService : IAdoptionApplicationService
    {
        private readonly AdoptAPetDbContext _context;

        public AdoptionApplicationService(AdoptAPetDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AdoptionApplicationDto>> GetAllApplicationsAsync()
        {
            return await _context.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.User)
                .Select(a => MapToDto(a))
                .ToListAsync();
        }

        public async Task<AdoptionApplicationDto?> GetApplicationByIdAsync(int id)
        {
            var application = await _context.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            return application != null ? MapToDto(application) : null;
        }

        public async Task<IEnumerable<AdoptionApplicationDto>> GetApplicationsByPetIdAsync(int petId)
        {
            return await _context.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.User)
                .Where(a => a.PetId == petId)
                .Select(a => MapToDto(a))
                .ToListAsync();
        }

        public async Task<IEnumerable<AdoptionApplicationDto>> GetApplicationsByStatusAsync(ApplicationStatus status)
        {
            return await _context.AdoptionApplications
                .Include(a => a.Pet)
                .Include(a => a.User)
                .Where(a => a.Status == status)
                .Select(a => MapToDto(a))
                .ToListAsync();
        }

        public async Task<AdoptionApplicationDto> CreateApplicationAsync(CreateAdoptionApplicationDto createApplicationDto)
        {
            // Check if pet exists
            var pet = await _context.Pets.FindAsync(createApplicationDto.PetId);
            if (pet == null)
                throw new ArgumentException($"Pet with ID {createApplicationDto.PetId} not found.");

            // Check if user exists
            var user = await _context.Users.FindAsync(createApplicationDto.UserId);
            if (user == null)
                throw new ArgumentException($"User with ID {createApplicationDto.UserId} not found.");

            // Check if pet is available
            if (pet.Status != PetStatus.Available)
                throw new ArgumentException($"Pet with ID {createApplicationDto.PetId} is not available for adoption.");

            var application = new AdoptionApplication
            {
                PetId = createApplicationDto.PetId,
                UserId = createApplicationDto.UserId,
                ApplicantName = createApplicationDto.ApplicantName,
                ApplicantEmail = createApplicationDto.ApplicantEmail,
                ApplicantPhone = createApplicationDto.ApplicantPhone,
                ApplicantAddress = createApplicationDto.ApplicantAddress,
                AdditionalNotes = createApplicationDto.AdditionalNotes,
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow
            };

            _context.AdoptionApplications.Add(application);

            // Update pet status to pending
            pet.Status = PetStatus.Pending;

            await _context.SaveChangesAsync();

            return MapToDto(application);
        }

        public async Task<AdoptionApplicationDto?> UpdateApplicationAsync(int id, UpdateAdoptionApplicationDto updateApplicationDto)
        {        
            // 1. Update specified fields
            // 2. If status is changed to approved, update pet status to Adopted and reject other applications
            // 3. If status is changed to rejected and no more pending applications, change pet status to Available
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteApplicationAsync(int id)
        {
            var application = await _context.AdoptionApplications
                .Include(a => a.Pet)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
                return false;

            _context.AdoptionApplications.Remove(application);

            // If this was the only pending application for the pet, set pet status back to Available
            if (application.Status == ApplicationStatus.Pending && application.Pet?.Status == PetStatus.Pending)
            {
                var hasPendingApplications = await _context.AdoptionApplications
                    .AnyAsync(a => a.PetId == application.PetId && a.Id != application.Id && a.Status == ApplicationStatus.Pending);

                if (!hasPendingApplications)
                {
                    application.Pet.Status = PetStatus.Available;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private static AdoptionApplicationDto MapToDto(AdoptionApplication application)
        {
            return new AdoptionApplicationDto
            {
                Id = application.Id,
                PetId = application.PetId,
                PetName = application.Pet?.Name ?? string.Empty,
                UserId = application.UserId,
                ApplicantName = application.ApplicantName,
                ApplicantEmail = application.ApplicantEmail,
                ApplicantPhone = application.ApplicantPhone,
                ApplicantAddress = application.ApplicantAddress,
                AdditionalNotes = application.AdditionalNotes,
                Status = application.Status,
                ApplicationDate = application.ApplicationDate,
                LastUpdated = application.LastUpdated
            };
        }
    }
}