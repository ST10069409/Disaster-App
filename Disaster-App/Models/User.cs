using System.ComponentModel.DataAnnotations;

namespace Disaster_App.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Role { get; set; } = "User";

        [Phone]
        public string? Phone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<Incident>? Incidents { get; set; }
        public Volunteer? VolunteerProfile { get; set; }
    }
}