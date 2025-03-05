using AdoptAPet.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace AdoptAPet.Api.DTOs
{
    public class AdoptionApplicationDto
    {
        public int Id { get; set; }
        public int PetId { get; set; }
        public string PetName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string ApplicantPhone { get; set; } = string.Empty;
        public string ApplicantAddress { get; set; } = string.Empty;
        public string? AdditionalNotes { get; set; }
        public ApplicationStatus Status { get; set; }
        public DateTime ApplicationDate { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class CreateAdoptionApplicationDto
    {
        [Required]
        public int PetId { get; set; }

        [Required]
        public int UserId { get; set; }

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
    }

    public class UpdateAdoptionApplicationDto
    {
        [MaxLength(100)]
        public string? ApplicantName { get; set; }
        
        [MaxLength(100)]
        [EmailAddress]
        public string? ApplicantEmail { get; set; }
        
        [MaxLength(20)]
        public string? ApplicantPhone { get; set; }
        
        [MaxLength(200)]
        public string? ApplicantAddress { get; set; }
        
        [MaxLength(1000)]
        public string? AdditionalNotes { get; set; }
        
        public ApplicationStatus? Status { get; set; }
    }
} 