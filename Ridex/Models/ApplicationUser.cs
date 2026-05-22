using Microsoft.AspNetCore.Identity;

namespace Ridex.Models
{
    public class ApplicationUser : IdentityUser
    {

        public string FullName { get; set; }
        public string RoleName { get; set; }
        public string? ProfilePhoto { get; set; }
    }
}