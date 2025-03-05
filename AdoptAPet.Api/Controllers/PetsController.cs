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
        public async Task<ActionResult<PaginatedResponseDto<PetDto>>> GetAllPets(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1)
                return BadRequest("Page number must be greater than 0");

            if (pageSize < 1 || pageSize > 100)
                return BadRequest("Page size must be between 1 and 100");

            var result = await _petService.GetPaginatedPetsAsync(pageNumber, pageSize);
            return Ok(result);
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
            try
            {
                var pet = await _petService.CreatePetAsync(createPetDto);
                return CreatedAtAction(nameof(GetPetById), new { id = pet.Id }, pet);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while creating the pet: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ShelterStaff")]
        public async Task<ActionResult<PetDto>> UpdatePet(int id, UpdatePetDto updatePetDto)
        {
            try
            {
                var pet = await _petService.UpdatePetAsync(id, updatePetDto);
                if (pet == null)
                    return NotFound();

                return Ok(pet);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating the pet: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,ShelterStaff")]
        public async Task<ActionResult> DeletePet(int id)
        {
            try
            {
                var result = await _petService.DeletePetAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting the pet: {ex.Message}");
            }
        }
    }
} 