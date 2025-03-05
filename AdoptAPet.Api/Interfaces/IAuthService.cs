using AdoptAPet.Api.DTOs;

namespace AdoptAPet.Api.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
        Task<bool> UserExistsAsync(string username);
    }
} 