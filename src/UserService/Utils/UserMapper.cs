using UserService.Models.Entities;
using UserService.Models.DTOs;

namespace UserService.Utils
{
    public static class UserMapper
    {
        public static User ToEntity(RegisterRequestDto dto, string cognitoSub)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                CognitoSub = cognitoSub,
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                CreatedAt = DateTime.UtcNow
            };
        }

        public static UserResponseDto ToResponseDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
