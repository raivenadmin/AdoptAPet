using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Interfaces;
using AdoptAPet.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdoptAPet.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdoptionApplicationsController : ControllerBase
    {
        private readonly IAdoptionApplicationService _applicationService;
        private readonly IPetService _petService;

        public AdoptionApplicationsController(IAdoptionApplicationService applicationService, IPetService petService)
        {
            _applicationService = applicationService;
            _petService = petService;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<AdoptionApplicationDto>>> GetAllApplications()
        {
            // TODO: Implement this method with the following requirements:
            // 1. Return all applications with role-based filtering:
            //    - Admin can view all applications
            //    - ShelterStaff can only view applications for their shelter
            //    - Adopters can only view their own applications
            // 2. Use proper authorization and error handling
            // 3. Return appropriate HTTP status codes
            
            throw new NotImplementedException("This method needs to be implemented");
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<AdoptionApplicationDto>> GetApplicationById(int id)
        {
            try
            {
                var application = await _applicationService.GetApplicationByIdAsync(id);
                if (application == null)
                    return NotFound();

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Check if user has permission to view this application
                if (userRole == UserRole.Adopter.ToString() && application.UserId != userId)
                {
                    return Forbid();
                }
                else if (userRole == UserRole.ShelterStaff.ToString())
                {
                    var shelterId = int.Parse(User.FindFirst("ShelterId")?.Value ?? "0");
                    if (shelterId == 0)
                        return Forbid();

                    var pet = await _petService.GetPetByIdAsync(application.PetId);
                    if (pet == null || pet.ShelterId != shelterId)
                        return Forbid();
                }

                return Ok(application);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the application: {ex.Message}");
            }
        }

        [HttpGet("pet/{petId}")]
        [Authorize(Roles = "Admin,ShelterStaff")]
        public async Task<ActionResult<IEnumerable<AdoptionApplicationDto>>> GetApplicationsByPetId(int petId)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // If user is shelter staff, check if the pet belongs to their shelter
                if (userRole == UserRole.ShelterStaff.ToString())
                {
                    var shelterId = int.Parse(User.FindFirst("ShelterId")?.Value ?? "0");
                    if (shelterId == 0)
                        return Forbid();

                    var pet = await _petService.GetPetByIdAsync(petId);
                    if (pet == null || pet.ShelterId != shelterId)
                        return Forbid();
                }

                var applications = await _applicationService.GetApplicationsByPetIdAsync(petId);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving applications: {ex.Message}");
            }
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<AdoptionApplicationDto>>> GetMyApplications()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Forbid();

                var applications = await _applicationService.GetAllApplicationsAsync();
                return Ok(applications.Where(a => a.UserId == userId).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving your applications: {ex.Message}");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Adopter")]
        public async Task<ActionResult<AdoptionApplicationDto>> CreateApplication(CreateAdoptionApplicationDto createApplicationDto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Forbid();

                // Ensure the application is created for the current user
                createApplicationDto.UserId = userId;

                var application = await _applicationService.CreateApplicationAsync(createApplicationDto);
                return CreatedAtAction(nameof(GetApplicationById), new { id = application.Id }, application);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while creating the application: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<AdoptionApplicationDto>> UpdateApplication(int id, UpdateAdoptionApplicationDto updateApplicationDto)
        {
            try
            {
                var application = await _applicationService.GetApplicationByIdAsync(id);
                if (application == null)
                    return NotFound();

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Check if user has permission to update this application
                if (userRole == UserRole.Adopter.ToString())
                {
                    if (application.UserId != userId)
                        return Forbid();

                    // Adopters can't change the status
                    updateApplicationDto.Status = null;
                }
                else if (userRole == UserRole.ShelterStaff.ToString())
                {
                    var shelterId = int.Parse(User.FindFirst("ShelterId")?.Value ?? "0");
                    if (shelterId == 0)
                        return Forbid();

                    var pet = await _petService.GetPetByIdAsync(application.PetId);
                    if (pet == null || pet.ShelterId != shelterId)
                        return Forbid();
                }

                var updatedApplication = await _applicationService.UpdateApplicationAsync(id, updateApplicationDto);
                return Ok(updatedApplication);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating the application: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteApplication(int id)
        {
            try
            {
                var result = await _applicationService.DeleteApplicationAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting the application: {ex.Message}");
            }
        }

        
    }
}