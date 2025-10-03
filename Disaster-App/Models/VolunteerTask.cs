using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Disaster_App.Models
{
    public class VolunteerTask
    {
        [Key]
        public int TaskID { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Task Name")]
        public string TaskName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Open"; // Open, Assigned, Completed

        public int? AssignedTo { get; set; }

        [ForeignKey("AssignedTo")]
        [Display(Name = "Assigned Volunteer")]
        public Volunteer? AssignedVolunteer { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}