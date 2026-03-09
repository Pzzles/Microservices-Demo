using UserService.Models.DTOs;

namespace UserService.Services
{
    public interface IUserService
    {
        Task<UserResponseDto> RegisterAsync(RegisterRequestDto dto);
        Task<UserResponseDto?> GetByIdAsync(Guid id);
        Task<string> GetAccessTokenAsync(LoginRequestDto dto);
    }
}
