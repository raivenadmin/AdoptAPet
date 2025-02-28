using AdoptAPet.Api.Data;
using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Interfaces;
using AdoptAPet.Api.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AdoptAPet.Tests
{
    public class Task1_ShelterServiceTests : ApiTestBase
    {
        private Shelter? _testShelter;

        public Task1_ShelterServiceTests() : base()
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
        }

        [Fact]
        public void ShelterService_IsRegisteredInDIContainer()
        {
            // Arrange & Act
            var shelterService = _scope.ServiceProvider.GetService<IShelterService>();

            // Assert
            shelterService.Should().NotBeNull("IShelterService should be registered in the DI container");
        }

        [Fact]
        public async Task ShelterService_GetAllSheltersAsync_ReturnsAllShelters()
        {
            // Arrange
            var shelterService = _scope.ServiceProvider.GetService<IShelterService>();
            shelterService.Should().NotBeNull("IShelterService should be registered in the DI container");
            
            // Act
            var shelters = await shelterService!.GetAllSheltersAsync();

            // Assert
            shelters.Should().NotBeNull();
            shelters.Should().HaveCountGreaterThan(0);
            shelters.Should().Contain(s => s.Name == "Test Shelter");
        }

        [Fact]
        public async Task ShelterService_GetShelterByIdAsync_ReturnsShelter()
        {
            // Arrange
            var shelterService = _scope.ServiceProvider.GetService<IShelterService>();
            shelterService.Should().NotBeNull("IShelterService should be registered in the DI container");
            
            // Act
            var shelter = await shelterService!.GetShelterByIdAsync(_testShelter!.Id);

            // Assert
            shelter.Should().NotBeNull();
            shelter!.Name.Should().Be("Test Shelter");
            shelter.Address.Should().Be("123 Test St");
        }

        [Fact]
        public async Task ShelterService_CreateShelterAsync_CreatesShelter()
        {
            // Arrange
            var shelterService = _scope.ServiceProvider.GetService<IShelterService>();
            shelterService.Should().NotBeNull("IShelterService should be registered in the DI container");
            
            var createShelterDto = new CreateShelterDto
            {
                Name = "New Test Shelter",
                Address = "456 New St",
                Phone = "555-987-6543",
                Email = "new@shelter.com"
            };

            // Act
            var createdShelter = await shelterService!.CreateShelterAsync(createShelterDto);

            // Assert
            createdShelter.Should().NotBeNull();
            createdShelter.Name.Should().Be(createShelterDto.Name);
            createdShelter.Address.Should().Be(createShelterDto.Address);
            createdShelter.Phone.Should().Be(createShelterDto.Phone);
            createdShelter.Email.Should().Be(createShelterDto.Email);

            // Verify it was actually saved to the database
            var shelterInDb = await _dbContext.Shelters.FindAsync(createdShelter.Id);
            shelterInDb.Should().NotBeNull();
            shelterInDb!.Name.Should().Be(createShelterDto.Name);
        }

        [Fact]
        public async Task ShelterService_UpdateShelterAsync_UpdatesShelter()
        {
            // Arrange
            var shelterService = _scope.ServiceProvider.GetService<IShelterService>();
            shelterService.Should().NotBeNull("IShelterService should be registered in the DI container");
            
            var updateShelterDto = new UpdateShelterDto
            {
                Name = "Updated Shelter Name",
                Address = "789 Updated St",
                Phone = "555-555-5555",
                Email = "updated@shelter.com"
            };

            // Act
            var updatedShelter = await shelterService!.UpdateShelterAsync(_testShelter!.Id, updateShelterDto);

            // Assert
            updatedShelter.Should().NotBeNull();
            updatedShelter!.Name.Should().Be(updateShelterDto.Name);
            updatedShelter.Address.Should().Be(updateShelterDto.Address);

            // Verify it was actually updated in the database
            var shelterInDb = await _dbContext.Shelters.FindAsync(_testShelter.Id);
            shelterInDb.Should().NotBeNull();
            shelterInDb!.Name.Should().Be(updateShelterDto.Name);
            shelterInDb.Address.Should().Be(updateShelterDto.Address);
        }

        [Fact]
        public async Task ShelterService_DeleteShelterAsync_DeletesShelter()
        {
            // Arrange
            var shelterService = _scope.ServiceProvider.GetService<IShelterService>();
            shelterService.Should().NotBeNull("IShelterService should be registered in the DI container");
            
            var shelterToDelete = new Shelter
            {
                Name = "Shelter To Delete",
                Address = "999 Delete St",
                Phone = "555-111-2222",
                Email = "delete@shelter.com"
            };
            _dbContext.Shelters.Add(shelterToDelete);
            await _dbContext.SaveChangesAsync();

            var shelterIdToDelete = shelterToDelete.Id;

            // Act
            var result = await shelterService!.DeleteShelterAsync(shelterIdToDelete);

            // Assert
            result.Should().BeTrue();

            // Verify it was actually deleted from the database
            var deletedShelter = await _dbContext.Shelters.FindAsync(shelterIdToDelete);
            deletedShelter.Should().BeNull();
        }
    }
} 