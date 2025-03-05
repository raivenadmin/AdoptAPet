using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AdoptAPet.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
        {
            if (await _authService.UserExistsAsync(registerDto.Username))
                return BadRequest("Username already exists");

            var response = await _authService.RegisterAsync(registerDto);
            if (response == null)
                return BadRequest("Registration failed");

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            var response = await _authService.LoginAsync(loginDto);
            if (response == null)
                return Unauthorized("Invalid username or password");

            return Ok(response);
        }
    }
} 