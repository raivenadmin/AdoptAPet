using AdoptAPet.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace AdoptAPet.Api.DTOs
{
    public class PetDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public PetType Type { get; set; }
        public string Breed { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? Description { get; set; }
        public PetStatus Status { get; set; }
        public DateTime DateAdded { get; set; }
        public int? ShelterId { get; set; }
        public string? ShelterName { get; set; }
    }

    public class CreatePetDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public PetType Type { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Breed { get; set; } = string.Empty;
        
        [Range(0, 30)]
        public int Age { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public int? ShelterId { get; set; }
    }

    public class UpdatePetDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }
        
        public PetType? Type { get; set; }
        
        [MaxLength(100)]
        public string? Breed { get; set; }
        
        [Range(0, 30)]
        public int? Age { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public PetStatus? Status { get; set; }
        
        public int? ShelterId { get; set; }
    }
} 