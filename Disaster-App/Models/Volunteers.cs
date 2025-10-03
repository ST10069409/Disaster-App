using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Disaster_App.Models
{
    public class Volunteer
    {
        [Key]
        public int VolunteerID { get; set; }

        [Required]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }

        [StringLength(200)]
        public string? Skills { get; set; }

        [StringLength(100)]
        public string? Availability { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.Now;

        // Navigation property
        public ICollection<VolunteerTask>? Tasks { get; set; }
    }
}