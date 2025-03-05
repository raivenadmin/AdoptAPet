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
            _adminToken = GetAuthTokenAsync(UserRole.Admin).Result;
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
        public async Task UpdatePet_ResolveBug()
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
            updatedPet!.ShelterName.Should().Be(_shelter2.Name);

        }


    }
} 