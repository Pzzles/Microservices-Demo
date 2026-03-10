using System.ComponentModel.DataAnnotations;

namespace UserService.Models.DTOs
{
    public class ConfirmRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
