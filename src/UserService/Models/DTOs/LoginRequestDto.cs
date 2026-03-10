using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UserService.Models.DTOs
{
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }
}
