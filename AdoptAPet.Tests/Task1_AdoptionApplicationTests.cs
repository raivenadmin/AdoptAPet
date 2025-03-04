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

            // Create a test user
            _testUser = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = new byte[] { },
                PasswordSalt = new byte[] { },
                Role = UserRole.Adopter,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(_testUser);
            await _dbContext.SaveChangesAsync();

            // Create a test pet
            _testPet = new Pet
            {
                Name = "Test Pet",
                Type = PetType.Dog,
                Breed = "Test Breed",
                Age = 2,
                Description = "A test pet",
                Status = PetStatus.Available,
                ShelterId = _testShelter.Id
            };
            _dbContext.Pets.Add(_testPet);
            await _dbContext.SaveChangesAsync();

            // Create a test application
            var application = new AdoptionApplication
            {
                PetId = _testPet.Id,
                UserId = _testUser.Id,
                ApplicantName = "Initial Applicant",
                ApplicantEmail = "initial@example.com",
                ApplicantPhone = "555-111-2222",
                ApplicantAddress = "456 Initial St",
                AdditionalNotes = "Initial notes",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow
            };
            _dbContext.AdoptionApplications.Add(application);
            await _dbContext.SaveChangesAsync();
        }

        [Fact]
        public void AdoptionApplicationService_IsRegisteredInDIContainer()
        {
            // Arrange & Act
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();

            // Assert
            applicationService.Should().NotBeNull("IAdoptionApplicationService should be registered in the DI container");
        }

        [Fact]
        public async Task AdoptionApplicationService_GetAllApplicationsAsync_ReturnsAllApplications()
        {
            // Arrange
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();
            applicationService.Should().NotBeNull("IAdoptionApplicationService should be registered in the DI container");
            
            // Act
            var applications = await applicationService!.GetAllApplicationsAsync();

            // Assert
            applications.Should().NotBeNull();
            applications.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task AdoptionApplicationService_GetApplicationByIdAsync_ReturnsApplication()
        {
            // Arrange
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();
            applicationService.Should().NotBeNull("IAdoptionApplicationService should be registered in the DI container");
            
            var existingApplication = await _dbContext.AdoptionApplications.FirstOrDefaultAsync();
            existingApplication.Should().NotBeNull();

            // Act
            var application = await applicationService!.GetApplicationByIdAsync(existingApplication!.Id);

            // Assert
            application.Should().NotBeNull();
            application!.ApplicantName.Should().Be(existingApplication.ApplicantName);
            application.UserId.Should().Be(existingApplication.UserId);
        }

        [Fact]
        public async Task AdoptionApplicationService_CreateApplicationAsync_CreatesApplication()
        {
            // Arrange
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();
            applicationService.Should().NotBeNull("IAdoptionApplicationService should be registered in the DI container");
            
            var createApplicationDto = new CreateAdoptionApplicationDto
            {
                PetId = _testPet!.Id,
                UserId = _testUser!.Id,
                ApplicantName = "Test Applicant",
                ApplicantEmail = "test@example.com",
                ApplicantPhone = "555-123-4567",
                ApplicantAddress = "123 Test St",
                AdditionalNotes = "I would love to adopt this pet!"
            };

            // Act
            var createdApplication = await applicationService!.CreateApplicationAsync(createApplicationDto);

            // Assert
            createdApplication.Should().NotBeNull();
            createdApplication.ApplicantName.Should().Be(createApplicationDto.ApplicantName);
            createdApplication.Status.Should().Be(ApplicationStatus.Pending);
            createdApplication.UserId.Should().Be(_testUser!.Id);

            // Verify it was actually saved to the database
            var applicationInDb = await _dbContext.AdoptionApplications.FindAsync(createdApplication.Id);
            applicationInDb.Should().NotBeNull();
            applicationInDb!.ApplicantName.Should().Be(createApplicationDto.ApplicantName);
            applicationInDb.UserId.Should().Be(_testUser!.Id);
        }

        [Fact]
        public async Task AdoptionApplicationService_UpdateApplicationAsync_UpdatesApplication()
        {
            // Arrange
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();
            applicationService.Should().NotBeNull("IAdoptionApplicationService should be registered in the DI container");
            
            var existingApplication = await _dbContext.AdoptionApplications.FirstOrDefaultAsync();
            existingApplication.Should().NotBeNull();

            var updateApplicationDto = new UpdateAdoptionApplicationDto
            {
                ApplicantPhone = "555-987-6543",
                AdditionalNotes = "Updated notes"
            };

            // Act
            var updatedApplication = await applicationService!.UpdateApplicationAsync(existingApplication!.Id, updateApplicationDto);

            // Assert
            updatedApplication.Should().NotBeNull();
            updatedApplication!.ApplicantPhone.Should().Be(updateApplicationDto.ApplicantPhone);
            updatedApplication.AdditionalNotes.Should().Be(updateApplicationDto.AdditionalNotes);
            updatedApplication.UserId.Should().Be(existingApplication.UserId);

            // Verify it was actually updated in the database
            var applicationInDb = await _dbContext.AdoptionApplications.FindAsync(existingApplication.Id);
            applicationInDb.Should().NotBeNull();
            applicationInDb!.ApplicantPhone.Should().Be(updateApplicationDto.ApplicantPhone);
            applicationInDb.AdditionalNotes.Should().Be(updateApplicationDto.AdditionalNotes);
            applicationInDb.UserId.Should().Be(existingApplication.UserId);
        }

        [Fact]
        public async Task AdoptionApplicationService_ApproveApplicationAsync_ApprovesApplication()
        {
            // Arrange
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();
            applicationService.Should().NotBeNull("IAdoptionApplicationService should be registered in the DI container");
            
            var existingApplication = await _dbContext.AdoptionApplications.FirstOrDefaultAsync();
            existingApplication.Should().NotBeNull();

            // Act
            var result = await applicationService!.ApproveApplicationAsync(existingApplication!.Id);

            // Assert
            result.Should().BeTrue();

            // Verify it was actually updated in the database
            var applicationInDb = await _dbContext.AdoptionApplications.FindAsync(existingApplication.Id);
            applicationInDb.Should().NotBeNull();
            applicationInDb!.Status.Should().Be(ApplicationStatus.Approved);
            applicationInDb.UserId.Should().Be(existingApplication.UserId);
        }

        [Fact]
        public async Task AdoptionApplicationService_RejectApplicationAsync_RejectsApplication()
        {
            // Arrange
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();
            applicationService.Should().NotBeNull("IAdoptionApplicationService should be registered in the DI container");
            
            var existingApplication = await _dbContext.AdoptionApplications.FirstOrDefaultAsync();
            existingApplication.Should().NotBeNull();

            // Act
            var result = await applicationService!.RejectApplicationAsync(existingApplication!.Id);

            // Assert
            result.Should().BeTrue();

            // Verify it was actually updated in the database
            var applicationInDb = await _dbContext.AdoptionApplications.FindAsync(existingApplication.Id);
            applicationInDb.Should().NotBeNull();
            applicationInDb!.Status.Should().Be(ApplicationStatus.Rejected);
            applicationInDb.UserId.Should().Be(existingApplication.UserId);
        }

        [Fact]
        public async Task AdoptionApplicationService_DeleteApplicationAsync_DeletesApplication()
        {
            // Arrange
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();
            applicationService.Should().NotBeNull("IAdoptionApplicationService should be registered in the DI container");
            
            var newApplication = new AdoptionApplication
            {
                PetId = _testPet!.Id,
                UserId = _testUser!.Id,
                ApplicantName = "Delete Test",
                ApplicantEmail = "delete@example.com",
                ApplicantPhone = "555-123-4567",
                ApplicantAddress = "123 Delete St",
                AdditionalNotes = "Delete me",
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.UtcNow
            };
            _dbContext.AdoptionApplications.Add(newApplication);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await applicationService!.DeleteApplicationAsync(newApplication.Id);

            // Assert
            result.Should().BeTrue();

            // Verify it was actually deleted from the database
            var deletedApplication = await _dbContext.AdoptionApplications.FindAsync(newApplication.Id);
            deletedApplication.Should().BeNull();
        }


        [Fact]
        public void AllRequiredServices_ShouldBeRegisteredInDIContainer()
        {
            // Arrange & Act
            var shelterService = _scope.ServiceProvider.GetService<IShelterService>();
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();
            var petService = _scope.ServiceProvider.GetService<IPetService>();
            var authService = _scope.ServiceProvider.GetService<IAuthService>();

            // Assert
            shelterService.Should().NotBeNull("IShelterService should be registered in the DI container");
            applicationService.Should().NotBeNull("IAdoptionApplicationService should be registered in the DI container");
            petService.Should().NotBeNull("IPetService should be registered in the DI container");
            authService.Should().NotBeNull("IAuthService should be registered in the DI container");
        }
    }
} 