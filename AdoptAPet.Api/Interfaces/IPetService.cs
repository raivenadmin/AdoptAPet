using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Models;

namespace AdoptAPet.Api.Interfaces
{
    public interface IPetService
    {
        Task<IEnumerable<PetDto>> GetAllPetsAsync();
        Task<PetDto?> GetPetByIdAsync(int id);
        Task<PaginatedResponseDto<PetDto>> GetPaginatedPetsAsync(int pageNumber, int pageSize);
        Task<IEnumerable<PetDto>> GetPetsByTypeAsync(PetType type);
        Task<IEnumerable<PetDto>> GetPetsByStatusAsync(PetStatus status);
        Task<IEnumerable<PetDto>> GetPetsByShelterAsync(int shelterId);
        Task<PetDto> CreatePetAsync(CreatePetDto createPetDto);
        Task<PetDto?> UpdatePetAsync(int id, UpdatePetDto updatePetDto);
        Task<bool> DeletePetAsync(int id);
    }
} 