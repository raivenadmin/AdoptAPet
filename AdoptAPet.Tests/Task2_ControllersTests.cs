using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace AdoptAPet.Tests
{
    public class Task2_ControllersTests : ApiTestBase
    {
        private string _adminToken;
        private string _shelterStaffToken;
        private string _adopterToken;

        public Task2_ControllersTests() : base()
        {
            // Get tokens for authorization
            _adminToken = GetAuthTokenAsync(UserRole.Admin).Result;
            _shelterStaffToken = GetAuthTokenAsync(UserRole.ShelterStaff).Result;
            _adopterToken = GetAuthTokenAsync(UserRole.Adopter).Result;
        }


        [Fact]
        public async Task AdoptionApplicationsController_GetAll_AdminCanAccess()
        {
            // Arrange
            SetAuthToken(_adminToken);

            // Act
            var response = await _client.GetAsync("/api/AdoptionApplications");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var applications = await response.Content.ReadFromJsonAsync<IEnumerable<AdoptionApplicationDto>>(_jsonOptions);
            applications.Should().NotBeNull();
        }

        [Fact]
        public async Task AdoptionApplicationsController_Create_AdopterCanCreate()
        {
            // Arrange
            SetAuthToken(_adopterToken);

            // Ensure we have a valid pet with ID 1 in the database
            var pet = await _dbContext.Pets.FindAsync(1);
            if (pet == null)
            {
                // Create a test shelter if it doesn't exist
                var shelter = await _dbContext.Shelters.FindAsync(1);
                if (shelter == null)
                {
                    shelter = new Shelter
                    {
                        Name = "Test Shelter",
                        Address = "123 Test St",
                        Phone = "555-123-4567",
                        Email = "test@shelter.com"
                    };
                    _dbContext.Shelters.Add(shelter);
                    await _dbContext.SaveChangesAsync();
                }

                // Create a test pet
                pet = new Pet
                {
                    Name = "Test Pet",
                    Type = PetType.Dog,
                    Breed = "Mixed",
                    Age = 2,
                    Description = "A friendly test pet",
                    Status = PetStatus.Available,
                    ShelterId = shelter.Id
                };
                _dbContext.Pets.Add(pet);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                // Ensure the pet is available
                pet.Status = PetStatus.Available;
                await _dbContext.SaveChangesAsync();
            }

            var newApplication = new CreateAdoptionApplicationDto
            {
                PetId = pet.Id,
                ApplicantName = "Test Applicant",
                ApplicantEmail = "test@example.com",
                ApplicantPhone = "555-123-4567",
                ApplicantAddress = "123 Test St",
                AdditionalNotes = "I would love to adopt this pet!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/AdoptionApplications", newApplication);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task AdoptionApplicationsController_GetById_ShelterStaffCanOnlyAccessTheirShelterApplications()
        {
            // Arrange
            // Create a shelter and associate it with the shelter staff user
            var shelter = new Shelter
            {
                Name = "Staff Shelter",
                Address = "456 Staff St",
                Phone = "555-987-6543",
                Email = "staff@shelter.com"
            };
            _dbContext.Shelters.Add(shelter);
            await _dbContext.SaveChangesAsync();

            // Update the shelter staff user with the shelter ID
            var staffUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == UserRole.ShelterStaff);
            if (staffUser != null)
            {
                staffUser.ShelterId = shelter.Id;
                await _dbContext.SaveChangesAsync();
            }

            // Create a pet for this shelter
            var pet = new Pet
            {
                Name = "Shelter Pet",
                Type = PetType.Cat,
                Breed = "Siamese",
                Age = 3,
                Description = "A shelter pet",
                Status = PetStatus.Available,
                ShelterId = shelter.Id
            };
            _dbContext.Pets.Add(pet);
            await _dbContext.SaveChangesAsync();

            // Create an application for this pet
            var application = new AdoptionApplication
            {
                PetId = pet.Id,
                UserId = staffUser.Id,
                ApplicantName = "Staff Applicant",
                ApplicantEmail = "staff@example.com",
                ApplicantPhone = "555-987-6543",
                ApplicantAddress = "456 Staff St",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow,
                AdditionalNotes = "Staff application"
            };
            _dbContext.AdoptionApplications.Add(application);
            await _dbContext.SaveChangesAsync();

            // Create another shelter, pet, and application
            var otherShelter = new Shelter
            {
                Name = "Other Shelter",
                Address = "789 Other St",
                Phone = "555-111-2222",
                Email = "other@shelter.com"
            };
            _dbContext.Shelters.Add(otherShelter);
            await _dbContext.SaveChangesAsync();

            var otherPet = new Pet
            {
                Name = "Other Pet",
                Type = PetType.Dog,
                Breed = "Labrador",
                Age = 2,
                Description = "Another pet",
                Status = PetStatus.Available,
                ShelterId = otherShelter.Id
            };
            _dbContext.Pets.Add(otherPet);
            await _dbContext.SaveChangesAsync();

            var otherApplication = new AdoptionApplication
            {
                PetId = otherPet.Id,
                UserId = staffUser.Id,
                ApplicantName = "Other Applicant",
                ApplicantEmail = "other@example.com",
                ApplicantPhone = "555-111-2222",
                ApplicantAddress = "789 Other St",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow,
                AdditionalNotes = "Other application"
            };
            _dbContext.AdoptionApplications.Add(otherApplication);
            await _dbContext.SaveChangesAsync();

            // Get a new token for the shelter staff with the updated shelter ID
            _shelterStaffToken = GetAuthTokenAsync(UserRole.ShelterStaff).Result;
            SetAuthToken(_shelterStaffToken);

            // Act - Try to access the application for their shelter's pet
            var response1 = await _client.GetAsync($"/api/AdoptionApplications/{application.Id}");

            // Act - Try to access the application for another shelter's pet
            var response2 = await _client.GetAsync($"/api/AdoptionApplications/{otherApplication.Id}");

            // Assert
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            response2.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task AdoptionApplicationsController_GetByPetId_ReturnsApplicationsForPet()
        {
            // Arrange
            SetAuthToken(_adminToken);

            // Create a pet
            var shelter = await _dbContext.Shelters.FirstOrDefaultAsync();
            if (shelter == null)
            {
                shelter = new Shelter
                {
                    Name = "Test Shelter",
                    Address = "123 Test St",
                    Phone = "555-123-4567",
                    Email = "test@shelter.com"
                };
                _dbContext.Shelters.Add(shelter);
                await _dbContext.SaveChangesAsync();
            }

            var pet = new Pet
            {
                Name = "Application Test Pet",
                Type = PetType.Dog,
                Breed = "Mixed",
                Age = 2,
                Description = "A pet for application testing",
                Status = PetStatus.Available,
                ShelterId = shelter.Id
            };
            _dbContext.Pets.Add(pet);
            await _dbContext.SaveChangesAsync();

            // Create multiple applications for this pet
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Adopter);

            for (int i = 0; i < 3; i++)
            {
                var application = new AdoptionApplication
                {
                    PetId = pet.Id,
                    UserId = user.Id,
                    ApplicantName = $"Applicant {i}",
                    ApplicantEmail = $"applicant{i}@example.com",
                    ApplicantPhone = $"555-123-456{i}",
                    ApplicantAddress = $"123 Test St Apt {i}",
                    Status = ApplicationStatus.Pending,
                    ApplicationDate = DateTime.UtcNow,
                    AdditionalNotes = $"Application {i}"
                };
                _dbContext.AdoptionApplications.Add(application);
            }
            await _dbContext.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync($"/api/AdoptionApplications/pet/{pet.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var applications = await response.Content.ReadFromJsonAsync<IEnumerable<AdoptionApplicationDto>>(_jsonOptions);
            applications.Should().NotBeNull();
            applications.Should().HaveCount(3);
            applications.Should().AllSatisfy(a => a.PetId.Should().Be(pet.Id));
        }

        [Fact]
        public async Task AdoptionApplicationsController_GetMyApplications_ReturnsUserApplications()
        {
            // Arrange
            // Get the adopter user
            var adopter = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "adopter@example.com");
            Console.WriteLine($"Adopter user ID: {adopter?.Id}, Email: {adopter?.Email}");

            // Get a fresh token for this specific user
            var adopterToken = await GetAuthTokenAsync(UserRole.Adopter);
            SetAuthToken(adopterToken);
            Console.WriteLine($"Using token: {adopterToken}");

            // Create multiple pets
            var shelter = await _dbContext.Shelters.FirstOrDefaultAsync();
            var pets = new List<Pet>();

            for (int i = 0; i < 2; i++)
            {
                var pet = new Pet
                {
                    Name = $"My App Pet {i}",
                    Type = PetType.Dog,
                    Breed = "Mixed",
                    Age = 2,
                    Description = $"Pet {i} for my applications test",
                    Status = PetStatus.Available,
                    ShelterId = shelter.Id
                };
                _dbContext.Pets.Add(pet);
                await _dbContext.SaveChangesAsync();
                pets.Add(pet);
                Console.WriteLine($"Created pet with ID: {pet.Id}");
            }

            // Create applications for these pets
            foreach (var pet in pets)
            {
                var application = new AdoptionApplication
                {
                    PetId = pet.Id,
                    UserId = adopter.Id,
                    ApplicantName = "Adopter Test",
                    ApplicantEmail = "adopter@example.com",
                    ApplicantPhone = "555-123-4567",
                    ApplicantAddress = "123 Adopter St",
                    Status = ApplicationStatus.Pending,
                    ApplicationDate = DateTime.UtcNow,
                    AdditionalNotes = $"Application for {pet.Name}"
                };
                _dbContext.AdoptionApplications.Add(application);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"Created application with ID: {application.Id} for pet: {pet.Id}, user: {application.UserId}");
            }

            // Verify applications were created
            var createdApplications = await _dbContext.AdoptionApplications
                .Where(a => a.UserId == adopter.Id)
                .ToListAsync();
            Console.WriteLine($"Number of applications created for user {adopter.Id}: {createdApplications.Count}");

            // Act
            var response = await _client.GetAsync("/api/AdoptionApplications/my");
            Console.WriteLine($"Response status code: {response.StatusCode}");

            // Get response content for debugging
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response content: {responseContent}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var applications = await response.Content.ReadFromJsonAsync<IEnumerable<AdoptionApplicationDto>>(_jsonOptions);
            applications.Should().NotBeNull();
            Console.WriteLine($"Number of applications returned: {applications?.Count() ?? 0}");
            applications.Should().HaveCountGreaterThanOrEqualTo(2);
            applications.Should().AllSatisfy(a =>
            {
                Console.WriteLine($"Application user ID: {a.UserId}, expected: {adopter.Id}");
                a.UserId.Should().Be(adopter.Id);
            });
        }

        [Fact]
        public async Task AdoptionApplicationsController_Update_AdopterCanUpdateOwnApplication()
        {
            // Arrange
            // Get the adopter user
            var adopter = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == "adopter@example.com");
            Console.WriteLine($"Adopter user ID: {adopter?.Id}, Email: {adopter?.Email}");

            // Get a fresh token for this specific user
            var adopterToken = await GetAuthTokenAsync(UserRole.Adopter);
            SetAuthToken(adopterToken);
            Console.WriteLine($"Using token: {adopterToken}");

            // Create a pet
            var shelter = await _dbContext.Shelters.FirstOrDefaultAsync();
            var pet = new Pet
            {
                Name = "Update Test Pet",
                Type = PetType.Dog,
                Breed = "Mixed",
                Age = 2,
                Description = "A pet for update testing",
                Status = PetStatus.Available,
                ShelterId = shelter.Id
            };
            _dbContext.Pets.Add(pet);
            await _dbContext.SaveChangesAsync();
            Console.WriteLine($"Created pet with ID: {pet.Id}");

            // Create an application
            var application = new AdoptionApplication
            {
                PetId = pet.Id,
                UserId = adopter.Id,
                ApplicantName = "Original Name",
                ApplicantEmail = "original@example.com",
                ApplicantPhone = "555-123-4567",
                ApplicantAddress = "123 Original St",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow,
                AdditionalNotes = "Original notes"
            };
            _dbContext.AdoptionApplications.Add(application);
            await _dbContext.SaveChangesAsync();
            Console.WriteLine($"Created application with ID: {application.Id} for user: {application.UserId}");

            // Create update DTO
            var updateDto = new UpdateAdoptionApplicationDto
            {
                ApplicantName = "Updated Name",
                ApplicantEmail = "updated@example.com",
                ApplicantPhone = "555-987-6543",
                ApplicantAddress = "456 Updated St",
                AdditionalNotes = "Updated notes"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/AdoptionApplications/{application.Id}", updateDto);
            Console.WriteLine($"Response status code: {response.StatusCode}");

            // Get response content for debugging
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response content: {responseContent}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // If the response was successful, verify the updated application
            if (response.IsSuccessStatusCode)
            {
                var updatedApplication = await response.Content.ReadFromJsonAsync<AdoptionApplicationDto>(_jsonOptions);
                updatedApplication.Should().NotBeNull();
                updatedApplication!.ApplicantName.Should().Be("Updated Name");
                updatedApplication.ApplicantEmail.Should().Be("updated@example.com");
                updatedApplication.ApplicantPhone.Should().Be("555-987-6543");
                updatedApplication.ApplicantAddress.Should().Be("456 Updated St");
                updatedApplication.AdditionalNotes.Should().Be("Updated notes");
                updatedApplication.Status.Should().Be(ApplicationStatus.Pending); // Status should not change
            }
            else
            {
                // If the response was not successful, print more details
                Console.WriteLine($"Failed to update application. Status: {response.StatusCode}");
                Console.WriteLine($"Response content: {responseContent}");
            }
        }

        [Fact]
        public async Task AdoptionApplicationsController_Delete_OnlyAdminCanDelete()
        {
            // Arrange
            // Create a pet and application
            var shelter = await _dbContext.Shelters.FirstOrDefaultAsync();
            var pet = new Pet
            {
                Name = "Delete Test Pet",
                Type = PetType.Dog,
                Breed = "Mixed",
                Age = 2,
                Description = "A pet for delete testing",
                Status = PetStatus.Available,
                ShelterId = shelter.Id
            };
            _dbContext.Pets.Add(pet);
            await _dbContext.SaveChangesAsync();
            Console.WriteLine($"Created pet with ID: {pet.Id}");

            var adopter = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Adopter);
            var application = new AdoptionApplication
            {
                PetId = pet.Id,
                UserId = adopter.Id,
                ApplicantName = "Delete Test",
                ApplicantEmail = "delete@example.com",
                ApplicantPhone = "555-123-4567",
                ApplicantAddress = "123 Delete St",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow,
                AdditionalNotes = "Delete test application"
            };
            _dbContext.AdoptionApplications.Add(application);
            await _dbContext.SaveChangesAsync();
            Console.WriteLine($"Created application with ID: {application.Id}");

            // Verify application exists
            var initialApplication = await _dbContext.AdoptionApplications.FindAsync(application.Id);
            Console.WriteLine($"Initial application exists: {initialApplication != null}");

            // Try to delete as adopter
            SetAuthToken(_adopterToken);
            var adopterResponse = await _client.DeleteAsync($"/api/AdoptionApplications/{application.Id}");
            Console.WriteLine($"Adopter delete response: {adopterResponse.StatusCode}");

            // Try to delete as shelter staff
            SetAuthToken(_shelterStaffToken);
            var staffResponse = await _client.DeleteAsync($"/api/AdoptionApplications/{application.Id}");
            Console.WriteLine($"Staff delete response: {staffResponse.StatusCode}");

            // Try to delete as admin
            SetAuthToken(_adminToken);
            var adminResponse = await _client.DeleteAsync($"/api/AdoptionApplications/{application.Id}");
            Console.WriteLine($"Admin delete response: {adminResponse.StatusCode}");

            // Assert
            adopterResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            staffResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            adminResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Force a database refresh
            _dbContext.ChangeTracker.Clear();

            // Verify the application is deleted
            var deletedApplication = await _dbContext.AdoptionApplications.FindAsync(application.Id);
            Console.WriteLine($"Application still exists after delete: {deletedApplication != null}");
            deletedApplication.Should().BeNull();
        }

        [Fact]
        public async Task AdoptionApplicationsController_Approve_ShelterStaffCanApproveTheirShelterApplications()
        {
            // Arrange
            // Create a shelter and associate it with the shelter staff user
            var shelter = new Shelter
            {
                Name = "Approve Test Shelter",
                Address = "456 Approve St",
                Phone = "555-987-6543",
                Email = "approve@shelter.com"
            };
            _dbContext.Shelters.Add(shelter);
            await _dbContext.SaveChangesAsync();

            // Update the shelter staff user with the shelter ID
            var staffUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == UserRole.ShelterStaff);
            if (staffUser != null)
            {
                staffUser.ShelterId = shelter.Id;
                await _dbContext.SaveChangesAsync();
            }

            // Create a pet for this shelter
            var pet = new Pet
            {
                Name = "Approve Test Pet",
                Type = PetType.Dog,
                Breed = "Golden Retriever",
                Age = 2,
                Description = "A pet for approval testing",
                Status = PetStatus.Available,
                ShelterId = shelter.Id
            };
            _dbContext.Pets.Add(pet);
            await _dbContext.SaveChangesAsync();

            // Create an application for this pet
            var adopter = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Adopter);
            var application = new AdoptionApplication
            {
                PetId = pet.Id,
                UserId = adopter.Id,
                ApplicantName = "Approve Test",
                ApplicantEmail = "approve@example.com",
                ApplicantPhone = "555-987-6543",
                ApplicantAddress = "456 Approve St",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow,
                AdditionalNotes = "Approve test application"
            };
            _dbContext.AdoptionApplications.Add(application);
            await _dbContext.SaveChangesAsync();

            // Check initial status
            var initialApplication = await _dbContext.AdoptionApplications.FindAsync(application.Id);
            Console.WriteLine($"Initial application status: {initialApplication?.Status}");

            // Get a new token for the shelter staff with the updated shelter ID
            _shelterStaffToken = GetAuthTokenAsync(UserRole.ShelterStaff).Result;
            SetAuthToken(_shelterStaffToken);

            // Act
            var response = await _client.PostAsync($"/api/AdoptionApplications/{application.Id}/approve", null);
            Console.WriteLine($"Response status code: {response.StatusCode}");

            // Get response content for debugging
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response content: {responseContent}");

            // Force a database refresh
            _dbContext.ChangeTracker.Clear();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the application is approved
            var approvedApplication = await _dbContext.AdoptionApplications.FindAsync(application.Id);
            Console.WriteLine($"Final application status: {approvedApplication?.Status}");
            approvedApplication.Should().NotBeNull();
            approvedApplication!.Status.Should().Be(ApplicationStatus.Approved);

            // Verify the pet status is updated
            var updatedPet = await _dbContext.Pets.FindAsync(pet.Id);
            Console.WriteLine($"Final pet status: {updatedPet?.Status}");
            updatedPet.Should().NotBeNull();
            updatedPet!.Status.Should().Be(PetStatus.Adopted);
        }

        [Fact]
        public async Task AdoptionApplicationsController_Reject_ShelterStaffCanRejectTheirShelterApplications()
        {
            // Arrange
            // Create a shelter and associate it with the shelter staff user
            var shelter = new Shelter
            {
                Name = "Reject Test Shelter",
                Address = "456 Reject St",
                Phone = "555-987-6543",
                Email = "reject@shelter.com"
            };
            _dbContext.Shelters.Add(shelter);
            await _dbContext.SaveChangesAsync();

            // Update the shelter staff user with the shelter ID
            var staffUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == UserRole.ShelterStaff);
            if (staffUser != null)
            {
                staffUser.ShelterId = shelter.Id;
                await _dbContext.SaveChangesAsync();
            }

            // Create a pet for this shelter
            var pet = new Pet
            {
                Name = "Reject Test Pet",
                Type = PetType.Cat,
                Breed = "Siamese",
                Age = 3,
                Description = "A pet for rejection testing",
                Status = PetStatus.Available,
                ShelterId = shelter.Id
            };
            _dbContext.Pets.Add(pet);
            await _dbContext.SaveChangesAsync();

            // Create an application for this pet
            var adopter = await _dbContext.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Adopter);
            var application = new AdoptionApplication
            {
                PetId = pet.Id,
                UserId = adopter.Id,
                ApplicantName = "Reject Test",
                ApplicantEmail = "reject@example.com",
                ApplicantPhone = "555-987-6543",
                ApplicantAddress = "456 Reject St",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow,
                AdditionalNotes = "Reject test application"
            };
            _dbContext.AdoptionApplications.Add(application);
            await _dbContext.SaveChangesAsync();

            // Check initial status
            var initialApplication = await _dbContext.AdoptionApplications.FindAsync(application.Id);
            Console.WriteLine($"Initial application status: {initialApplication?.Status}");

            // Get a new token for the shelter staff with the updated shelter ID
            _shelterStaffToken = GetAuthTokenAsync(UserRole.ShelterStaff).Result;
            SetAuthToken(_shelterStaffToken);

            // Act
            var response = await _client.PostAsync($"/api/AdoptionApplications/{application.Id}/reject", null);
            Console.WriteLine($"Response status code: {response.StatusCode}");

            // Get response content for debugging
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response content: {responseContent}");

            // Force a database refresh
            _dbContext.ChangeTracker.Clear();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify the application is rejected
            var rejectedApplication = await _dbContext.AdoptionApplications.FindAsync(application.Id);
            Console.WriteLine($"Final application status: {rejectedApplication?.Status}");
            rejectedApplication.Should().NotBeNull();
            rejectedApplication!.Status.Should().Be(ApplicationStatus.Rejected);
        }
    }
}