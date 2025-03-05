using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AdoptAPet.Tests
{
    public class Task2_GetAllApplicationsTests : ApiTestBase
    {
        private string _adminToken;
        private string _shelterStaffToken;
        private string _adopterToken;
        private int _staffShelterId;
        private int _staffUserId;
        private int _adopterUserId;
        private int _staffShelterPetId;
        private int _otherShelterPetId;

        public Task2_GetAllApplicationsTests() : base()
        {
            SetupTestData().Wait();
            _adminToken = GetAuthTokenAsync(UserRole.Admin).Result;
            _shelterStaffToken = GetAuthTokenAsync(UserRole.ShelterStaff).Result;
            _adopterToken = GetAuthTokenAsync(UserRole.Adopter).Result;
        }

        private async Task SetupTestData()
        {
            // Create test users if they don't exist
            var adminUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin);
            var staffUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == UserRole.ShelterStaff);
            var adopterUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == "johnsmith" && u.Role == UserRole.Adopter);

            if (adminUser == null)
            {
                adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = System.Text.Encoding.UTF8.GetBytes("hashed_password_123"),
                    PasswordSalt = System.Text.Encoding.UTF8.GetBytes("salt_123"),
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.Users.Add(adminUser);
                await _dbContext.SaveChangesAsync();
            }

            if (staffUser == null)
            {
                staffUser = new User
                {
                    Username = "staff",
                    Email = "staff@example.com",
                    PasswordHash = System.Text.Encoding.UTF8.GetBytes("hashed_password_123"),
                    PasswordSalt = System.Text.Encoding.UTF8.GetBytes("salt_123"),
                    Role = UserRole.ShelterStaff,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.Users.Add(staffUser);
                await _dbContext.SaveChangesAsync();
            }

            if (adopterUser == null)
            {
                adopterUser = new User
                {
                    Username = "johnsmith",
                    Email = "adopter@example.com",
                    PasswordHash = System.Text.Encoding.UTF8.GetBytes("hashed_password_123"),
                    PasswordSalt = System.Text.Encoding.UTF8.GetBytes("salt_123"),
                    Role = UserRole.Adopter,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.Users.Add(adopterUser);
                await _dbContext.SaveChangesAsync();
            }

            _staffUserId = staffUser.Id;
            _adopterUserId = adopterUser.Id;

            // Create a shelter for the staff user if it doesn't exist
            if (!staffUser.ShelterId.HasValue)
            {
                var shelter = new Shelter
                {
                    Name = "Staff Test Shelter",
                    Address = "123 Staff St",
                    Phone = "555-123-4567",
                    Email = "staff.shelter@example.com"
                };
                _dbContext.Shelters.Add(shelter);
                await _dbContext.SaveChangesAsync();

                staffUser.ShelterId = shelter.Id;
                await _dbContext.SaveChangesAsync();
            }

            _staffShelterId = staffUser.ShelterId.Value;

            // Create pets for both shelters
            var staffShelterPet = new Pet
            {
                Name = "Staff Shelter Pet",
                Type = PetType.Dog,
                Breed = "Golden Retriever",
                Age = 3,
                Description = "A friendly dog",
                Status = PetStatus.Available,
                ShelterId = _staffShelterId
            };
            _dbContext.Pets.Add(staffShelterPet);
            await _dbContext.SaveChangesAsync();
            _staffShelterPetId = staffShelterPet.Id;

            // Create another shelter
            var otherShelter = new Shelter
            {
                Name = "Other Test Shelter",
                Address = "456 Other St",
                Phone = "555-987-6543",
                Email = "other.shelter@example.com"
            };
            _dbContext.Shelters.Add(otherShelter);
            await _dbContext.SaveChangesAsync();

            var otherShelterPet = new Pet
            {
                Name = "Other Shelter Pet",
                Type = PetType.Cat,
                Breed = "Siamese",
                Age = 2,
                Description = "A friendly cat",
                Status = PetStatus.Available,
                ShelterId = otherShelter.Id
            };
            _dbContext.Pets.Add(otherShelterPet);
            await _dbContext.SaveChangesAsync();
            _otherShelterPetId = otherShelterPet.Id;

            // Create applications for both pets
            // 1. Application for staff shelter pet by adopter
            var staffShelterApplication = new AdoptionApplication
            {
                PetId = _staffShelterPetId,
                UserId = _adopterUserId,
                ApplicantName = "Adopter User",
                ApplicantEmail = "adopter@example.com",
                ApplicantPhone = "555-111-2222",
                ApplicantAddress = "789 Adopter St",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow.AddDays(-1),
                AdditionalNotes = "Application for staff shelter pet"
            };
            _dbContext.AdoptionApplications.Add(staffShelterApplication);

            // 2. Application for other shelter pet by adopter
            var otherShelterApplication = new AdoptionApplication
            {
                PetId = _otherShelterPetId,
                UserId = _adopterUserId,
                ApplicantName = "Adopter User",
                ApplicantEmail = "adopter@example.com",
                ApplicantPhone = "555-111-2222",
                ApplicantAddress = "789 Adopter St",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow,
                AdditionalNotes = "Application for other shelter pet"
            };
            _dbContext.AdoptionApplications.Add(otherShelterApplication);

            // 3. Application for staff shelter pet by another user
            var anotherUser = new User
            {
                Username = "another",
                Email = "another@example.com",
                PasswordHash = new byte[] { 1, 2, 3 },
                PasswordSalt = new byte[] { 4, 5, 6 },
                Role = UserRole.Adopter,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(anotherUser);
            await _dbContext.SaveChangesAsync();

            var anotherUserApplication = new AdoptionApplication
            {
                PetId = _staffShelterPetId,
                UserId = anotherUser.Id,
                ApplicantName = "Another User",
                ApplicantEmail = "another@example.com",
                ApplicantPhone = "555-333-4444",
                ApplicantAddress = "101 Another St",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow.AddDays(-2),
                AdditionalNotes = "Another application for staff shelter pet"
            };
            _dbContext.AdoptionApplications.Add(anotherUserApplication);

            await _dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task AdoptionApplicationsController_GetAll_AdminCanAccessAllApplications()
        {
            // Arrange
            SetAuthToken(_adminToken);

            // Act
            var response = await _client.GetAsync("/api/AdoptionApplications");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var applications = await response.Content.ReadFromJsonAsync<IEnumerable<AdoptionApplicationDto>>(_jsonOptions);
            applications.Should().NotBeNull();
            
            // Admin should see all applications
            applications!.Count().Should().BeGreaterThanOrEqualTo(3); // At least the 3 we created
        }

        [Fact]
        public async Task AdoptionApplicationsController_GetAll_ShelterStaffCanOnlyAccessTheirShelterApplications()
        {
            // Arrange
            SetAuthToken(_shelterStaffToken);

            // Act
            var response = await _client.GetAsync("/api/AdoptionApplications");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var applications = await response.Content.ReadFromJsonAsync<IEnumerable<AdoptionApplicationDto>>(_jsonOptions);
            applications.Should().NotBeNull();
            
            // Get the actual pet IDs from the database
            var staffShelterPets = await _dbContext.Pets
                .Where(p => p.ShelterId == _staffShelterId)
                .Select(p => p.Id)
                .ToListAsync();
            
            // ShelterStaff should only see applications for their shelter's pets
            applications!.All(a => staffShelterPets.Contains(a.PetId)).Should().BeTrue();
            
            // Should see applications for their shelter pets
            applications!.Should().NotBeEmpty();
        }

        [Fact]
        public async Task AdoptionApplicationsController_GetAll_AdopterCanOnlyAccessTheirOwnApplications()
        {
            // Arrange
            // Get the token for the johnsmith user who has applications
            var johnsmith = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == "johnsmith");
            if (johnsmith == null)
            {
                throw new Exception("Johnsmith user not found");
            }
            
            // Create a token for this user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, johnsmith.Id.ToString()),
                new Claim(ClaimTypes.Name, johnsmith.Username),
                new Claim(ClaimTypes.Email, johnsmith.Email),
                new Claim(ClaimTypes.Role, johnsmith.Role.ToString())
            };
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super_secret_key_for_jwt_token_generation_12345678901234567890"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);
            
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);
            
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            SetAuthToken(jwt);

            // Act
            var response = await _client.GetAsync("/api/AdoptionApplications");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var applications = await response.Content.ReadFromJsonAsync<IEnumerable<AdoptionApplicationDto>>(_jsonOptions);
            applications.Should().NotBeNull();
            
            // Adopter should only see their own applications
            applications!.All(a => a.UserId == johnsmith.Id).Should().BeTrue();
            
            // Should see their applications
            applications!.Should().NotBeEmpty();
        }

    }
} 