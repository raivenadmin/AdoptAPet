using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Interfaces;
using AdoptAPet.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdoptAPet.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PetsController : ControllerBase
    {
        private readonly IPetService _petService;

        public PetsController(IPetService petService)
        {
            _petService = petService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PetDto>>> GetAllPets()
        {
            var pets = await _petService.GetAllPetsAsync();
            return Ok(pets);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PetDto>> GetPetById(int id)
        {
            var pet = await _petService.GetPetByIdAsync(id);
            if (pet == null)
                return NotFound();

            return Ok(pet);
        }

        [HttpGet("type/{type}")]
        public async Task<ActionResult<IEnumerable<PetDto>>> GetPetsByType(PetType type)
        {
            var pets = await _petService.GetPetsByTypeAsync(type);
            return Ok(pets);
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<PetDto>>> GetPetsByStatus(PetStatus status)
        {
            var pets = await _petService.GetPetsByStatusAsync(status);
            return Ok(pets);
        }

        [HttpGet("shelter/{shelterId}")]
        public async Task<ActionResult<IEnumerable<PetDto>>> GetPetsByShelter(int shelterId)
        {
            var pets = await _petService.GetPetsByShelterAsync(shelterId);
            return Ok(pets);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ShelterStaff")]
        public async Task<ActionResult<PetDto>> CreatePet(CreatePetDto createPetDto)
        {
            var pet = await _petService.CreatePetAsync(createPetDto);
            return CreatedAtAction(nameof(GetPetById), new { id = pet.Id }, pet);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ShelterStaff")]
        public async Task<ActionResult<PetDto>> UpdatePet(int id, UpdatePetDto updatePetDto)
        {
            var pet = await _petService.UpdatePetAsync(id, updatePetDto);
            if (pet == null)
                return NotFound();

            return Ok(pet);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,ShelterStaff")]
        public async Task<ActionResult> DeletePet(int id)
        {
            var result = await _petService.DeletePetAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
} 