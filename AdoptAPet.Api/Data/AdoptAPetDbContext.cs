using AdoptAPet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AdoptAPet.Api.Data
{
    public class AdoptAPetDbContext : DbContext
    {
        public AdoptAPetDbContext(DbContextOptions<AdoptAPetDbContext> options) : base(options)
        {
        }

        public DbSet<Pet> Pets { get; set; }
        public DbSet<Shelter> Shelters { get; set; }
        public DbSet<AdoptionApplication> AdoptionApplications { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Pet>()
                .HasOne(p => p.Shelter)
                .WithMany(s => s.Pets)
                .HasForeignKey(p => p.ShelterId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdoptionApplication>()
                .HasOne(a => a.Pet)
                .WithMany(p => p.AdoptionApplications)
                .HasForeignKey(a => a.PetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Shelter)
                .WithMany()
                .HasForeignKey(u => u.ShelterId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AdoptionApplication>()
                .HasOne(a => a.User)
                .WithMany(u => u.AdoptionApplications)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed shelters
            modelBuilder.Entity<Shelter>().HasData(
                new Shelter { Id = 1, Name = "Happy Paws Shelter", Address = "123 Main St, Anytown, USA", Phone = "555-123-4567", Email = "info@happypaws.example.com" },
                new Shelter { Id = 2, Name = "Furry Friends Rescue", Address = "456 Oak Ave, Somewhere, USA", Phone = "555-987-6543", Email = "contact@furryfriendsrescue.example.com" }
            );

            // Seed users
            modelBuilder.Entity<User>().HasData(
                new User 
                { 
                    Id = 1, 
                    Username = "admin", 
                    Email = "admin@example.com", 
                    PasswordHash = new byte[] { }, // In a real app, this would be a proper hash
                    PasswordSalt = new byte[] { }, // In a real app, this would be a proper salt
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.Parse("2023-01-01")
                },
                new User 
                { 
                    Id = 2, 
                    Username = "johnsmith", 
                    Email = "john.smith@example.com", 
                    PasswordHash = new byte[] { }, // In a real app, this would be a proper hash
                    PasswordSalt = new byte[] { }, // In a real app, this would be a proper salt
                    Role = UserRole.Adopter,
                    CreatedAt = DateTime.Parse("2023-01-02")
                }
            );

            // Seed pets
            modelBuilder.Entity<Pet>().HasData(
                new Pet { Id = 1, Name = "Buddy", Type = PetType.Dog, Breed = "Golden Retriever", Age = 3, Description = "Friendly and energetic dog who loves to play fetch.", Status = PetStatus.Available, ShelterId = 1, DateAdded = DateTime.Parse("2023-01-15") },
                new Pet { Id = 2, Name = "Whiskers", Type = PetType.Cat, Breed = "Siamese", Age = 2, Description = "Quiet and affectionate cat who loves to cuddle.", Status = PetStatus.Available, ShelterId = 1, DateAdded = DateTime.Parse("2023-02-10") },
                new Pet { Id = 3, Name = "Max", Type = PetType.Dog, Breed = "German Shepherd", Age = 4, Description = "Loyal and protective dog, good with children.", Status = PetStatus.Pending, ShelterId = 2, DateAdded = DateTime.Parse("2023-01-20") },
                new Pet { Id = 4, Name = "Mittens", Type = PetType.Cat, Breed = "Maine Coon", Age = 1, Description = "Playful kitten who loves toys and attention.", Status = PetStatus.Available, ShelterId = 2, DateAdded = DateTime.Parse("2023-03-05") }
            );

            // Seed adoption applications
            modelBuilder.Entity<AdoptionApplication>().HasData(
                new AdoptionApplication
                {
                    Id = 1,
                    PetId = 3,
                    UserId = 2, // Link to John Smith's user account
                    ApplicantName = "John Smith",
                    ApplicantEmail = "john.smith@example.com",
                    ApplicantPhone = "555-111-2222",
                    ApplicantAddress = "789 Pine Rd, Anytown, USA",
                    AdditionalNotes = "I have a large yard and work from home.",
                    Status = ApplicationStatus.Pending,
                    ApplicationDate = DateTime.Parse("2023-03-10")
                }
            );
        }
    }
} 