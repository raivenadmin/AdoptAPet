using System.ComponentModel.DataAnnotations;

namespace AdoptAPet.Api.Models
{
    public enum PetStatus
    {
        Available,
        Pending,
        Adopted
    }

    public enum PetType
    {
        Dog,
        Cat,
        Bird,
        Rabbit,
        Other
    }

    public class Pet
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public PetType Type { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Breed { get; set; } = string.Empty;
        
        public int Age { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public PetStatus Status { get; set; } = PetStatus.Available;
        
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        
        public int? ShelterId { get; set; }
        public Shelter? Shelter { get; set; }
        
        public ICollection<AdoptionApplication> AdoptionApplications { get; set; } = new List<AdoptionApplication>();
    }
} 