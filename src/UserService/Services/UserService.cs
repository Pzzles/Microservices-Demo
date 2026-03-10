using UserService.Repositories;
using UserService.Models.DTOs;
using UserService.Utils;

namespace UserService.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICognitoService _cognitoService;

        public UserService(IUserRepository userRepository, ICognitoService cognitoService)
        {
            _userRepository = userRepository;
            _cognitoService = cognitoService;
        }

        public async Task<UserResponseDto> RegisterAsync(RegisterRequestDto dto)
        {
            if (dto is null)
            {
                throw new ArgumentException("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new ArgumentException("Name, email, and password are required.");
            }

            var cognitoSub = await _cognitoService.RegisterAsync(dto.Email, dto.Password, dto.Name);
            var user = UserMapper.ToEntity(dto, cognitoSub);

            await _userRepository.AddAsync(user);

            return UserMapper.ToResponseDto(user);
        }

        public async Task<UserResponseDto?> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user is null ? null : UserMapper.ToResponseDto(user);
        }

        public async Task<string> GetAccessTokenAsync(LoginRequestDto dto)
        {
            if (dto is null)
            {
                throw new ArgumentException("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new ArgumentException("Email and password are required.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                await _cognitoService.ConfirmAsync(dto.Email, dto.Code);
            }

            try
            {
                return await _cognitoService.AuthenticateAsync(dto.Email, dto.Password);
            }
            catch (ArgumentException ex) when (
                !string.IsNullOrWhiteSpace(dto.Code) &&
                ex.Message.Contains("not confirmed", StringComparison.OrdinalIgnoreCase))
            {
                await _cognitoService.ConfirmAsync(dto.Email, dto.Code!);
                return await _cognitoService.AuthenticateAsync(dto.Email, dto.Password);
            }
        }

        public async Task<string> ConfirmUserAsync(ConfirmRequestDto dto)
        {
            if (dto is null)
            {
                throw new ArgumentException("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Code))
            {
                throw new ArgumentException("Email and confirmation code are required.");
            }

            return await _cognitoService.ConfirmAsync(dto.Email, dto.Code);
        }
    }
}
