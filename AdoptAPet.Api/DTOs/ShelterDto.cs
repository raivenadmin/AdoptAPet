using System.ComponentModel.DataAnnotations;

namespace AdoptAPet.Api.DTOs
{
    public class ShelterDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int PetCount { get; set; }
    }

    public class CreateShelterDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string Address { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string? Phone { get; set; }
        
        [MaxLength(100)]
        [EmailAddress]
        public string? Email { get; set; }
    }

    public class UpdateShelterDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }
        
        [MaxLength(200)]
        public string? Address { get; set; }
        
        [MaxLength(20)]
        public string? Phone { get; set; }
        
        [MaxLength(100)]
        [EmailAddress]
        public string? Email { get; set; }
    }
} 