using AdoptAPet.Api.DTOs;

namespace AdoptAPet.Api.Interfaces
{
    public interface IShelterService
    {
        Task<IEnumerable<ShelterDto>> GetAllSheltersAsync();
        Task<ShelterDto?> GetShelterByIdAsync(int id);
        Task<ShelterDto> CreateShelterAsync(CreateShelterDto createShelterDto);
        Task<ShelterDto?> UpdateShelterAsync(int id, UpdateShelterDto updateShelterDto);
        Task<bool> DeleteShelterAsync(int id);
    }
} 