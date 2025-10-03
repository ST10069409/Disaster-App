using System.ComponentModel.DataAnnotations;

namespace Disaster_App.Models
{
    public class Donation
    {
        [Key]
        public int DonationID { get; set; }

        [Required(ErrorMessage = "Donor name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        [Display(Name = "Full Name")]
        public string DonorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Resource type is required")]
        [StringLength(100, ErrorMessage = "Resource type cannot exceed 100 characters")]
        [Display(Name = "Resource Type")]
        public string ResourceType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Additional Details")]
        public string? Description { get; set; }

        [Display(Name = "Donation Date")]
        public DateTime DonationDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        [Display(Name = "Pickup Address")]
        public string? PickupAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}