using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Models;

namespace AdoptAPet.Api.Interfaces
{
    public interface IAdoptionApplicationService
    {
        Task<IEnumerable<AdoptionApplicationDto>> GetAllApplicationsAsync();
        Task<AdoptionApplicationDto?> GetApplicationByIdAsync(int id);
        Task<IEnumerable<AdoptionApplicationDto>> GetApplicationsByPetIdAsync(int petId);
        Task<IEnumerable<AdoptionApplicationDto>> GetApplicationsByStatusAsync(ApplicationStatus status);
        Task<AdoptionApplicationDto> CreateApplicationAsync(CreateAdoptionApplicationDto createApplicationDto);
        Task<AdoptionApplicationDto?> UpdateApplicationAsync(int id, UpdateAdoptionApplicationDto updateApplicationDto);
        Task<bool> DeleteApplicationAsync(int id);
    }
} 