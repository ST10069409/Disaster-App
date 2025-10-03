using System.ComponentModel.DataAnnotations;

namespace Disaster_App.Models
{
    public class Incident
    {
        [Key]
        public int IncidentID { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(200)]
        public string? Location { get; set; }

        public DateTime DateReported { get; set; } = DateTime.Now;

        // Foreign Key
        public int ReportedBy { get; set; }
        public User? Reporter { get; set; }
    }
}