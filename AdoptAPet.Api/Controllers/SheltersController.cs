using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Interfaces;
using AdoptAPet.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdoptAPet.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SheltersController : ControllerBase
    {
        private readonly IShelterService _shelterService;

        public SheltersController(IShelterService shelterService)
        {
            _shelterService = shelterService;
        }

        // GET: api/Shelters
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShelterDto>>> GetAll()
        {
            var shelters = await _shelterService.GetAllSheltersAsync();
            return Ok(shelters);
        }

        // GET: api/Shelters/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ShelterDto>> GetById(int id)
        {
            var shelter = await _shelterService.GetShelterByIdAsync(id);
            if (shelter == null)
                return NotFound();

            return Ok(shelter);
        }

        // POST: api/Shelters
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ShelterDto>> Create(CreateShelterDto createShelterDto)
        {
            try
            {
                var shelter = await _shelterService.CreateShelterAsync(createShelterDto);
                return CreatedAtAction(nameof(GetById), new { id = shelter.Id }, shelter);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/Shelters/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ShelterDto>> Update(int id, UpdateShelterDto updateShelterDto)
        {
            try
            {
                var shelter = await _shelterService.UpdateShelterAsync(id, updateShelterDto);
                if (shelter == null)
                    return NotFound();

                return Ok(shelter);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/Shelters/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _shelterService.DeleteShelterAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
} 