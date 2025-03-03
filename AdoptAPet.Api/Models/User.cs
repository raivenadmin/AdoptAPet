using System.ComponentModel.DataAnnotations;

namespace AdoptAPet.Api.Models
{
    public enum UserRole
    {
        Admin,
        ShelterStaff,
        Adopter
    }

    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        
        [Required]
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
        
        [Required]
        public UserRole Role { get; set; } = UserRole.Adopter;
        
        public int? ShelterId { get; set; }
        public Shelter? Shelter { get; set; }
        
        public ICollection<AdoptionApplication> AdoptionApplications { get; set; } = new List<AdoptionApplication>();
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 