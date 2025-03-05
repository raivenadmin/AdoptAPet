using System.ComponentModel.DataAnnotations;

namespace AdoptAPet.Api.Models
{
    public class Shelter
    {
        [Key]
        public int Id { get; set; }
        
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
        
        public ICollection<Pet> Pets { get; set; } = new List<Pet>();
    }
} 