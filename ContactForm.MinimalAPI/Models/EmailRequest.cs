using System.ComponentModel.DataAnnotations;

namespace ContactForm.MinimalAPI.Models
{
    // MODEL FOR EMAIL REQUEST
    public class EmailRequest
    {
        [Required(ErrorMessage = "The Email field is required.")]
        [EmailAddress(ErrorMessage = "The Email field is not a valid e-mail address.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "The Username field is required.")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "The Message field is required.")]
        public string? Message { get; set; }
    }
}
