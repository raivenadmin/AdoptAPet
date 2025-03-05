using AdoptAPet.Api.Data;
using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Interfaces;
using AdoptAPet.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AdoptAPet.Api.Services
{
    public class PetService : IPetService
    {
        private readonly AdoptAPetDbContext _context;

        public PetService(AdoptAPetDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PetDto>> GetAllPetsAsync()
        {
            return await _context.Pets
                .Include(p => p.Shelter)
                .Select(p => MapToDto(p))
                .ToListAsync();
        }

        public async Task<PetDto?> GetPetByIdAsync(int id)
        {
            var pet = await _context.Pets
                .Include(p => p.Shelter)
                .FirstOrDefaultAsync(p => p.Id == id);

            return pet != null ? MapToDto(pet) : null;
        }

        public async Task<PaginatedResponseDto<PetDto>> GetPaginatedPetsAsync(int pageNumber, int pageSize)
        {

            // Get total count for pagination metadata
            var totalCount = await _context.Pets.CountAsync();

            // Calculate total pages
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply pagination
            var pets = await _context.Pets
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => MapToDto(p))
                .ToListAsync();

            // Create paginated response
            var result = new PaginatedResponseDto<PetDto>
            {
                Items = pets,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            return result;
        }

        public async Task<IEnumerable<PetDto>> GetPetsByTypeAsync(PetType type)
        {
            return await _context.Pets
                .Include(p => p.Shelter)
                .Where(p => p.Type == type)
                .Select(p => MapToDto(p))
                .ToListAsync();
        }

        public async Task<IEnumerable<PetDto>> GetPetsByStatusAsync(PetStatus status)
        {
            return await _context.Pets
                .Include(p => p.Shelter)
                .Where(p => p.Status == status)
                .Select(p => MapToDto(p))
                .ToListAsync();
        }

        public async Task<IEnumerable<PetDto>> GetPetsByShelterAsync(int shelterId)
        {
            return await _context.Pets
                .Include(p => p.Shelter)
                .Where(p => p.ShelterId == shelterId)
                .Select(p => MapToDto(p))
                .ToListAsync();
        }

        public async Task<PetDto> CreatePetAsync(CreatePetDto createPetDto)
        {
            var pet = new Pet
            {
                Name = createPetDto.Name,
                Type = createPetDto.Type,
                Breed = createPetDto.Breed,
                Age = createPetDto.Age,
                Description = createPetDto.Description,
                ShelterId = createPetDto.ShelterId,
                Status = PetStatus.Available,
                DateAdded = DateTime.UtcNow
            };

            _context.Pets.Add(pet);
            await _context.SaveChangesAsync();

            return MapToDto(pet);
        }

        public async Task<PetDto?> UpdatePetAsync(int id, UpdatePetDto updatePetDto)
        {
            var pet = await _context.Pets
                .Include(p => p.Shelter)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pet == null)
                return null;

            if (updatePetDto.Name != null)
                pet.Name = updatePetDto.Name;

            if (updatePetDto.Type.HasValue)
                pet.Type = updatePetDto.Type.Value;

            if (updatePetDto.Breed != null)
                pet.Breed = updatePetDto.Breed;

            if (updatePetDto.Age.HasValue)
                pet.Age = updatePetDto.Age.Value;

            if (updatePetDto.Description != null)
                pet.Description = updatePetDto.Description;

            if (updatePetDto.Status.HasValue)
                pet.Status = updatePetDto.Status.Value;

            if (updatePetDto.ShelterId.HasValue)
                pet.ShelterId = updatePetDto.ShelterId;

            await _context.SaveChangesAsync();

            return MapToDto(pet);
        }

        public async Task<bool> DeletePetAsync(int id)
        {
            var pet = await _context.Pets.FindAsync(id);
            if (pet == null)
                return false;

            _context.Pets.Remove(pet);
            await _context.SaveChangesAsync();
            return true;
        }

        private static PetDto MapToDto(Pet pet)
        {
            return new PetDto
            {
                Id = pet.Id,
                Name = pet.Name,
                Type = pet.Type,
                Breed = pet.Breed,
                Age = pet.Age,
                Description = pet.Description,
                Status = pet.Status,
                DateAdded = pet.DateAdded,
                ShelterId = pet.ShelterId,
                ShelterName = pet.Shelter?.Name
            };
        }
    }
}