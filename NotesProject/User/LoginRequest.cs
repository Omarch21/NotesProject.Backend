using System.ComponentModel.DataAnnotations;

namespace NotesProject.User
{
    public class LoginRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

    }
}
