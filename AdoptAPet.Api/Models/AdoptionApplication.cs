using System.ComponentModel.DataAnnotations;

namespace AdoptAPet.Api.Models
{
    public enum ApplicationStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class AdoptionApplication
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int PetId { get; set; }
        public Pet? Pet { get; set; }
        
        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string ApplicantName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string ApplicantEmail { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string ApplicantPhone { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string ApplicantAddress { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? AdditionalNotes { get; set; }
        
        [Required]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        
        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastUpdated { get; set; }
    }
} 