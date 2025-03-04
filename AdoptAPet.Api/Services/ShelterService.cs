using AdoptAPet.Api.Data;
using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Interfaces;
using AdoptAPet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoptAPet.Api.Services
{
    public class ShelterService : IShelterService
    {
        private readonly AdoptAPetDbContext _context;

        public ShelterService(AdoptAPetDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ShelterDto>> GetAllSheltersAsync()
        {
            return await _context.Shelters
                .Select(s => new ShelterDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    Phone = s.Phone,
                    Email = s.Email
                })
                .ToListAsync();
        }

        public async Task<ShelterDto?> GetShelterByIdAsync(int id)
        {
            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null)
                return null;

            return new ShelterDto
            {
                Id = shelter.Id,
                Name = shelter.Name,
                Address = shelter.Address,
                Phone = shelter.Phone,
                Email = shelter.Email
            };
        }

        public async Task<ShelterDto> CreateShelterAsync(CreateShelterDto createShelterDto)
        {
            var shelter = new Shelter
            {
                Name = createShelterDto.Name,
                Address = createShelterDto.Address,
                Phone = createShelterDto.Phone,
                Email = createShelterDto.Email
            };

            _context.Shelters.Add(shelter);
            await _context.SaveChangesAsync();

            return new ShelterDto
            {
                Id = shelter.Id,
                Name = shelter.Name,
                Address = shelter.Address,
                Phone = shelter.Phone,
                Email = shelter.Email
            };
        }

        public async Task<ShelterDto?> UpdateShelterAsync(int id, UpdateShelterDto updateShelterDto)
        {
            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null)
                return null;

            if (updateShelterDto.Name != null)
                shelter.Name = updateShelterDto.Name;
            
            if (updateShelterDto.Address != null)
                shelter.Address = updateShelterDto.Address;
            
            if (updateShelterDto.Phone != null)
                shelter.Phone = updateShelterDto.Phone;
            
            if (updateShelterDto.Email != null)
                shelter.Email = updateShelterDto.Email;

            await _context.SaveChangesAsync();

            return new ShelterDto
            {
                Id = shelter.Id,
                Name = shelter.Name,
                Address = shelter.Address,
                Phone = shelter.Phone,
                Email = shelter.Email
            };
        }

        public async Task<bool> DeleteShelterAsync(int id)
        {
            var shelter = await _context.Shelters.FindAsync(id);
            if (shelter == null)
                return false;

            _context.Shelters.Remove(shelter);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 