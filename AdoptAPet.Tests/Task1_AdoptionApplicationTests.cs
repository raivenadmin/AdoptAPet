using AdoptAPet.Api.Data;
using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Interfaces;
using AdoptAPet.Api.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AdoptAPet.Tests
{
    public class Task1_AdoptionApplicationTests : ApiTestBase
    {
        private Shelter? _testShelter;
        private Pet? _testPet;
        private User? _testUser;

        public Task1_AdoptionApplicationTests() : base()
        {
            SetupTestData().Wait();
        }

        private async Task SetupTestData()
        {
            // Create a test shelter
            _testShelter = new Shelter
            {
                Name = "Test Shelter",
                Address = "123 Test St",
                Phone = "555-123-4567",
                Email = "test@shelter.com"
            };
            _dbContext.Shelters.Add(_testShelter);
            await _dbContext.SaveChangesAsync();

            // Create a test pet
            _testPet = new Pet
            {
                Name = "Test Pet",
                Type = PetType.Dog,
                Breed = "Mixed",
                Age = 3,
                Description = "A test pet",
                Status = PetStatus.Available,
                ShelterId = _testShelter.Id
            };
            _dbContext.Pets.Add(_testPet);
            await _dbContext.SaveChangesAsync();

            // Create a test user
            _testUser = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = new byte[] { 1, 2, 3 },
                PasswordSalt = new byte[] { 4, 5, 6 },
                Role = UserRole.Adopter,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(_testUser);
            await _dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateApplicationAsync_UpdatesBasicFields_WhenFieldsAreProvided()
        {
            // Arrange
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();
            
            // Create an application to update
            var application = new AdoptionApplication
            {
                PetId = _testPet!.Id,
                UserId = _testUser!.Id,
                ApplicantName = "Original Name",
                ApplicantEmail = "original@example.com",
                ApplicantPhone = "555-123-4567",
                ApplicantAddress = "123 Original St",
                AdditionalNotes = "Original notes",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow.AddDays(-1)
            };
            _dbContext.AdoptionApplications.Add(application);
            await _dbContext.SaveChangesAsync();

            var updateDto = new UpdateAdoptionApplicationDto
            {
                ApplicantName = "Updated Name",
                ApplicantEmail = "updated@example.com",
                ApplicantPhone = "555-987-6543",
                ApplicantAddress = "456 Updated St",
                AdditionalNotes = "Updated notes"
            };

            // Act
            var result = await applicationService!.UpdateApplicationAsync(application.Id, updateDto);

            // Assert
            result.Should().NotBeNull();
            result!.ApplicantName.Should().Be("Updated Name");
            result.ApplicantEmail.Should().Be("updated@example.com");
            result.ApplicantPhone.Should().Be("555-987-6543");
            result.ApplicantAddress.Should().Be("456 Updated St");
            result.AdditionalNotes.Should().Be("Updated notes");
            result.Status.Should().Be(ApplicationStatus.Pending); // Status should not change
            result.LastUpdated.Should().NotBeNull();
            
            // Verify database was updated
            var updatedApplication = await _dbContext.AdoptionApplications.FindAsync(application.Id);
            updatedApplication.Should().NotBeNull();
            updatedApplication!.ApplicantName.Should().Be("Updated Name");
            updatedApplication.ApplicantEmail.Should().Be("updated@example.com");
            updatedApplication.ApplicantPhone.Should().Be("555-987-6543");
            updatedApplication.ApplicantAddress.Should().Be("456 Updated St");
            updatedApplication.AdditionalNotes.Should().Be("Updated notes");
        }

        [Fact]
        public async Task UpdateApplicationAsync_ApprovesApplication_UpdatesPetStatusAndRejectsOtherApplications()
        {
            // Arrange
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();
            
            // Create a pet for testing
            var pet = new Pet
            {
                Name = "Approval Test Pet",
                Type = PetType.Dog,
                Breed = "Golden Retriever",
                Age = 2,
                Description = "A pet for approval testing",
                Status = PetStatus.Available,
                ShelterId = _testShelter!.Id
            };
            _dbContext.Pets.Add(pet);
            await _dbContext.SaveChangesAsync();
            
            // Create multiple applications for the same pet
            var application1 = new AdoptionApplication
            {
                PetId = pet.Id,
                UserId = _testUser!.Id,
                ApplicantName = "Applicant 1",
                ApplicantEmail = "applicant1@example.com",
                ApplicantPhone = "555-111-1111",
                ApplicantAddress = "111 Applicant St",
                AdditionalNotes = "First application",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow.AddDays(-2)
            };
            
            var application2 = new AdoptionApplication
            {
                PetId = pet.Id,
                UserId = _testUser!.Id,
                ApplicantName = "Applicant 2",
                ApplicantEmail = "applicant2@example.com",
                ApplicantPhone = "555-222-2222",
                ApplicantAddress = "222 Applicant St",
                AdditionalNotes = "Second application",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow.AddDays(-1)
            };
            
            _dbContext.AdoptionApplications.Add(application1);
            _dbContext.AdoptionApplications.Add(application2);
            await _dbContext.SaveChangesAsync();
            
            var updateDto = new UpdateAdoptionApplicationDto
            {
                Status = ApplicationStatus.Approved
            };

            // Act
            var result = await applicationService!.UpdateApplicationAsync(application1.Id, updateDto);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(ApplicationStatus.Approved);
            
            // Verify pet status was updated to Adopted
            _dbContext.ChangeTracker.Clear(); // Clear tracking to ensure fresh data
            var updatedPet = await _dbContext.Pets.FindAsync(pet.Id);
            updatedPet.Should().NotBeNull();
            updatedPet!.Status.Should().Be(PetStatus.Adopted);
            
            // Verify other applications were rejected
            var otherApplication = await _dbContext.AdoptionApplications.FindAsync(application2.Id);
            otherApplication.Should().NotBeNull();
            otherApplication!.Status.Should().Be(ApplicationStatus.Rejected);
        }

        [Fact]
        public async Task UpdateApplicationAsync_RejectsApplication_UpdatesPetStatusIfNoOtherPendingApplications()
        {
            // Arrange
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();
            
            // Create a pet with Pending status
            var pet = new Pet
            {
                Name = "Rejection Test Pet",
                Type = PetType.Cat,
                Breed = "Siamese",
                Age = 3,
                Description = "A pet for rejection testing",
                Status = PetStatus.Pending, // Pet has pending applications
                ShelterId = _testShelter!.Id
            };
            _dbContext.Pets.Add(pet);
            await _dbContext.SaveChangesAsync();
            
            // Create a single application for this pet
            var application = new AdoptionApplication
            {
                PetId = pet.Id,
                UserId = _testUser!.Id,
                ApplicantName = "Reject Test",
                ApplicantEmail = "reject@example.com",
                ApplicantPhone = "555-333-3333",
                ApplicantAddress = "333 Reject St",
                AdditionalNotes = "Application to reject",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow.AddDays(-1)
            };
            
            _dbContext.AdoptionApplications.Add(application);
            await _dbContext.SaveChangesAsync();
            
            var updateDto = new UpdateAdoptionApplicationDto
            {
                Status = ApplicationStatus.Rejected
            };

            // Act
            var result = await applicationService!.UpdateApplicationAsync(application.Id, updateDto);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(ApplicationStatus.Rejected);
            
            // Verify pet status was updated to Available since there are no more pending applications
            _dbContext.ChangeTracker.Clear(); // Clear tracking to ensure fresh data
            var updatedPet = await _dbContext.Pets.FindAsync(pet.Id);
            updatedPet.Should().NotBeNull();
            updatedPet!.Status.Should().Be(PetStatus.Available);
        }

        [Fact]
        public async Task UpdateApplicationAsync_RejectsApplication_DoesNotUpdatePetStatusIfOtherPendingApplicationsExist()
        {
            // Arrange
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();
            
            // Create a pet with Pending status
            var pet = new Pet
            {
                Name = "Multiple Applications Pet",
                Type = PetType.Dog,
                Breed = "Labrador",
                Age = 1,
                Description = "A pet with multiple applications",
                Status = PetStatus.Pending, // Pet has pending applications
                ShelterId = _testShelter!.Id
            };
            _dbContext.Pets.Add(pet);
            await _dbContext.SaveChangesAsync();
            
            // Create multiple applications for the same pet
            var application1 = new AdoptionApplication
            {
                PetId = pet.Id,
                UserId = _testUser!.Id,
                ApplicantName = "Applicant 1",
                ApplicantEmail = "applicant1@example.com",
                ApplicantPhone = "555-111-1111",
                ApplicantAddress = "111 Applicant St",
                AdditionalNotes = "First application",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow.AddDays(-2)
            };
            
            var application2 = new AdoptionApplication
            {
                PetId = pet.Id,
                UserId = _testUser!.Id,
                ApplicantName = "Applicant 2",
                ApplicantEmail = "applicant2@example.com",
                ApplicantPhone = "555-222-2222",
                ApplicantAddress = "222 Applicant St",
                AdditionalNotes = "Second application",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow.AddDays(-1)
            };
            
            _dbContext.AdoptionApplications.Add(application1);
            _dbContext.AdoptionApplications.Add(application2);
            await _dbContext.SaveChangesAsync();
            
            var updateDto = new UpdateAdoptionApplicationDto
            {
                Status = ApplicationStatus.Rejected
            };

            // Act
            var result = await applicationService!.UpdateApplicationAsync(application1.Id, updateDto);

            // Assert
            result.Should().NotBeNull();
            result!.Status.Should().Be(ApplicationStatus.Rejected);
            
            // Verify pet status remains Pending since there are still other pending applications
            _dbContext.ChangeTracker.Clear(); // Clear tracking to ensure fresh data
            var updatedPet = await _dbContext.Pets.FindAsync(pet.Id);
            updatedPet.Should().NotBeNull();
            updatedPet!.Status.Should().Be(PetStatus.Pending);
            
            // Verify other application is still pending
            var otherApplication = await _dbContext.AdoptionApplications.FindAsync(application2.Id);
            otherApplication.Should().NotBeNull();
            otherApplication!.Status.Should().Be(ApplicationStatus.Pending);
        }

        [Fact]
        public async Task UpdateApplicationAsync_ReturnsNull_WhenApplicationDoesNotExist()
        {
            // Arrange
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();
            
            var updateDto = new UpdateAdoptionApplicationDto
            {
                ApplicantName = "Updated Name"
            };

            // Act
            var result = await applicationService!.UpdateApplicationAsync(999, updateDto);

            // Assert
            result.Should().BeNull();
        }
    }
} 