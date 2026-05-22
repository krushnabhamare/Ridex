using System.ComponentModel.DataAnnotations;

namespace Ridex.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}