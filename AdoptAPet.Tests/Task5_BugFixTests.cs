using AdoptAPet.Api.Data;
using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AdoptAPet.Tests
{
    public class Task5_BugFixTests : ApiTestBase
    {
        private string _adminToken;
        private Shelter? _shelter1;
        private Shelter? _shelter2;
        private Pet? _testPet;

        public Task5_BugFixTests() : base()
        {
            _adminToken = GetAuthTokenAsync("admin@example.com", "Password123!", "Admin").Result;
            SetupTestData().Wait();
        }

        private async Task SetupTestData()
        {
            // Create two test shelters
            _shelter1 = new Shelter
            {
                Name = "Bug Fix Test Shelter 1",
                Address = "123 Bug Fix St",
                Phone = "555-123-4567",
                Email = "bugfix1@shelter.com"
            };
            _dbContext.Shelters.Add(_shelter1);
            await _dbContext.SaveChangesAsync();

            _shelter2 = new Shelter
            {
                Name = "Bug Fix Test Shelter 2",
                Address = "456 Bug Fix St",
                Phone = "555-987-6543",
                Email = "bugfix2@shelter.com"
            };
            _dbContext.Shelters.Add(_shelter2);
            await _dbContext.SaveChangesAsync();

            // Create a test pet in the first shelter
            _testPet = new Pet
            {
                Name = "Bug Fix Pet",
                Type = PetType.Dog,
                Breed = "Bug Fix Breed",
                Age = 3,
                Description = "A test pet for bug fix",
                Status = PetStatus.Available,
                ShelterId = _shelter1.Id
            };
            _dbContext.Pets.Add(_testPet);
            await _dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task UpdatePet_WithShelterId_UpdatesShelterRelationship()
        {
            // Arrange
            SetAuthToken(_adminToken);
            var updatePet = new UpdatePetDto
            {
                Name = "Updated Bug Fix Pet",
                ShelterId = _shelter2!.Id // Change the shelter
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Pets/{_testPet!.Id}", updatePet);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedPet = await response.Content.ReadFromJsonAsync<PetDto>(_jsonOptions);
            updatedPet.Should().NotBeNull();
            updatedPet!.Name.Should().Be("Updated Bug Fix Pet");
            updatedPet.ShelterId.Should().Be(_shelter2.Id);
        }

        [Fact]
        public async Task UpdatePet_WithMultipleChanges_UpdatesAllFields()
        {
            // Arrange
            SetAuthToken(_adminToken);
            var updatePet = new UpdatePetDto
            {
                Name = "Fully Updated Pet",
                Type = PetType.Cat,
                Breed = "Updated Breed",
                Age = 5,
                Description = "Fully updated description",
                Status = PetStatus.Adopted,
                ShelterId = _shelter2!.Id
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Pets/{_testPet!.Id}", updatePet);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedPet = await response.Content.ReadFromJsonAsync<PetDto>(_jsonOptions);
            updatedPet.Should().NotBeNull();
            updatedPet!.Name.Should().Be("Fully Updated Pet");
            updatedPet.Type.Should().Be(PetType.Cat);
            updatedPet.Breed.Should().Be("Updated Breed");
            updatedPet.Age.Should().Be(5);
            updatedPet.Description.Should().Be("Fully updated description");
            updatedPet.Status.Should().Be(PetStatus.Adopted);
            updatedPet.ShelterId.Should().Be(_shelter2.Id);
        }

        [Fact]
        public async Task UpdatePet_WithInvalidShelterId_ReturnsBadRequest()
        {
            // Arrange
            SetAuthToken(_adminToken);
            var updatePet = new UpdatePetDto
            {
                Name = "Invalid Shelter Pet",
                ShelterId = 9999 // Non-existent shelter ID
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Pets/{_testPet!.Id}", updatePet);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdatePet_WithoutShelterId_PreservesOriginalShelter()
        {
            // Arrange
            SetAuthToken(_adminToken);
            var updatePet = new UpdatePetDto
            {
                Name = "Preserved Shelter Pet",
                Description = "Updated description without changing shelter"
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Pets/{_testPet!.Id}", updatePet);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedPet = await response.Content.ReadFromJsonAsync<PetDto>(_jsonOptions);
            updatedPet.Should().NotBeNull();
            updatedPet!.Name.Should().Be("Preserved Shelter Pet");
            updatedPet.ShelterId.Should().Be(_shelter1!.Id); // Original shelter should be preserved
        }

        [Fact]
        public async Task UpdatePet_WithNullShelterId_ReturnsBadRequest()
        {
            // Arrange
            SetAuthToken(_adminToken);
            var updatePet = new UpdatePetDto
            {
                Name = "Null Shelter Pet",
                ShelterId = null // Explicitly set to null
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/Pets/{_testPet!.Id}", updatePet);

            // Assert
            // The API should either preserve the original shelter or return a bad request
            // depending on how the bug fix is implemented
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var updatedPet = await response.Content.ReadFromJsonAsync<PetDto>(_jsonOptions);
                updatedPet.Should().NotBeNull();
                updatedPet!.ShelterId.Should().NotBe(0); // Shelter ID should not be 0 or default
            }
        }
    }
} 