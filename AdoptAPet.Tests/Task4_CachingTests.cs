using AdoptAPet.Api.Data;
using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using System.Text.Json;

namespace AdoptAPet.Tests
{
    public class Task4_CachingTests : ApiTestBase
    {
        private string _adminToken;

        public Task4_CachingTests() : base()
        {
            _adminToken = GetAuthTokenAsync(UserRole.Admin).Result;
            SetupTestData().Wait();
        }

        private async Task SetupTestData()
        {
            // Create a test shelter
            var shelter = new Shelter
            {
                Name = "Caching Test Shelter",
                Address = "123 Caching St",
                Phone = "555-123-4567",
                Email = "caching@shelter.com"
            };
            _dbContext.Shelters.Add(shelter);
            await _dbContext.SaveChangesAsync();

            // Create 10 test pets
            for (int i = 0; i < 10; i++)
            {
                var pet = new Pet
                {
                    Name = $"Caching Pet {i}",
                    Type = PetType.Dog,
                    Breed = $"Breed {i}",
                    Age = i + 1,
                    Description = $"Description for pet {i}",
                    Status = PetStatus.Available,
                    ShelterId = shelter.Id
                };
                _dbContext.Pets.Add(pet);
            }
            await _dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task GetAllPets_WithCaching()
        {
            // Use a fresh client for this test
            using var client = _factory.CreateClient();
            
            // First call - should cache the results
            var response1 = await client.GetAsync("/api/Pets?pageSize=100");
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Read the content as a string first to avoid ObjectDisposedException
            var content1 = await response1.Content.ReadAsStringAsync();
            
            // Try to deserialize as PaginatedResponseDto first, then fall back to IEnumerable if that fails
            IEnumerable<PetDto> firstCallPets;
            try 
            {
                var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponseDto<PetDto>>(content1, _jsonOptions);
                firstCallPets = paginatedResponse?.Items ?? new List<PetDto>();
            }
            catch
            {
                firstCallPets = JsonSerializer.Deserialize<IEnumerable<PetDto>>(content1, _jsonOptions) ?? new List<PetDto>();
            }
            
            firstCallPets.Should().NotBeNull();
            var initialPetCount = firstCallPets.Count();

            // Add a new pet directly to the database (bypassing the API and cache)
            var newPet = new Pet
            {
                Name = "Cache Test Pet",
                Type = PetType.Dog,
                Breed = "Cache Test Breed",
                Age = 1,
                Description = "This pet was added to test caching",
                Status = PetStatus.Available,
                DateAdded = DateTime.UtcNow
            };
            
            _dbContext.Pets.Add(newPet);
            await _dbContext.SaveChangesAsync();
            
            // Verify the pet was added to the database
            var dbPetCount = await _dbContext.Pets.CountAsync();
            dbPetCount.Should().Be(initialPetCount + 1);

            // Second call - should return cached results without the new pet
            var response2 = await client.GetAsync("/api/Pets?pageSize=100");
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Read the content as a string first to avoid ObjectDisposedException
            var content2 = await response2.Content.ReadAsStringAsync();
            
            // Get the pets from the second call
            IEnumerable<PetDto> secondCallPets;
            try 
            {
                var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponseDto<PetDto>>(content2, _jsonOptions);
                secondCallPets = paginatedResponse?.Items ?? new List<PetDto>();
            }
            catch
            {
                secondCallPets = JsonSerializer.Deserialize<IEnumerable<PetDto>>(content2, _jsonOptions) ?? new List<PetDto>();
            }
            
            // If caching is working, the second call should return the same number of pets as the first call
            // and should not include the new pet we added directly to the database
            secondCallPets.Count().Should().Be(initialPetCount);
            secondCallPets.Should().NotContain(p => p.Name == "Cache Test Pet");
        }

        [Fact]
        public async Task GetPetById_WithCaching_SecondCallIsFaster()
        {
            // Use a fresh client for this test
            using var client = _factory.CreateClient();
            
            // Get a pet ID to use for testing
            var allPetsResponse = await client.GetAsync("/api/Pets");
            allPetsResponse.EnsureSuccessStatusCode();
            
            // Read the content as a string first to avoid ObjectDisposedException
            var allPetsContent = await allPetsResponse.Content.ReadAsStringAsync();
            
            // Try to deserialize as PaginatedResponseDto first, then fall back to IEnumerable if that fails
            IEnumerable<PetDto> allPets;
            try 
            {
                var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponseDto<PetDto>>(allPetsContent, _jsonOptions);
                allPets = paginatedResponse?.Items ?? new List<PetDto>();
            }
            catch
            {
                allPets = JsonSerializer.Deserialize<IEnumerable<PetDto>>(allPetsContent, _jsonOptions) ?? new List<PetDto>();
            }
            
            if (allPets == null || !allPets.Any())
            {
                // Skip test if no pets are available
                return;
            }
            
            var petId = allPets.First().Id;

            // First call - should cache the result
            var response1 = await client.GetAsync($"/api/Pets/{petId}");
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Read the content as a string first to avoid ObjectDisposedException
            var content1 = await response1.Content.ReadAsStringAsync();
            var originalPet = JsonSerializer.Deserialize<PetDto>(content1, _jsonOptions);
            
            originalPet.Should().NotBeNull();
            var originalName = originalPet!.Name;

            // Modify the pet directly in the database (bypassing the API and cache)
            var dbPet = await _dbContext.Pets.FindAsync(petId);
            dbPet.Should().NotBeNull();
            dbPet!.Name = $"Modified Name {Guid.NewGuid()}"; // Ensure unique name
            await _dbContext.SaveChangesAsync();
            
            // Verify the pet was modified in the database
            var modifiedDbPet = await _dbContext.Pets.FindAsync(petId);
            modifiedDbPet!.Name.Should().NotBe(originalName);

            // Second call - should return cached result with the original name
            var response2 = await client.GetAsync($"/api/Pets/{petId}");
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Read the content as a string first to avoid ObjectDisposedException
            var content2 = await response2.Content.ReadAsStringAsync();
            var cachedPet = JsonSerializer.Deserialize<PetDto>(content2, _jsonOptions);
            
            // If caching is working, the second call should return the original pet data
            // and not reflect the changes made directly to the database
            cachedPet.Should().NotBeNull();
            cachedPet!.Name.Should().Be(originalName);
            cachedPet.Name.Should().NotBe(modifiedDbPet.Name);
        }

        [Fact]
        public async Task UpdatePet_InvalidatesCache()
        {
            // Use a fresh client for this test
            using var client = _factory.CreateClient();
            
            // Get a pet to update
            var allPetsResponse = await client.GetAsync("/api/Pets");
            allPetsResponse.EnsureSuccessStatusCode();
            
            // Read the content as a string first to avoid ObjectDisposedException
            var allPetsContent = await allPetsResponse.Content.ReadAsStringAsync();
            
            // Try to deserialize as PaginatedResponseDto first, then fall back to IEnumerable if that fails
            IEnumerable<PetDto> allPets;
            try 
            {
                var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponseDto<PetDto>>(allPetsContent, _jsonOptions);
                allPets = paginatedResponse?.Items ?? new List<PetDto>();
            }
            catch
            {
                allPets = JsonSerializer.Deserialize<IEnumerable<PetDto>>(allPetsContent, _jsonOptions) ?? new List<PetDto>();
            }
            
            if (allPets == null || !allPets.Any())
            {
                // Skip test if no pets are available
                return;
            }
            
            var pet = allPets.First();
            var originalName = pet.Name;

            // First call to cache the pet
            var response1 = await client.GetAsync($"/api/Pets/{pet.Id}");
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Read the content as a string first to avoid ObjectDisposedException
            var content1 = await response1.Content.ReadAsStringAsync();
            var firstCallPet = JsonSerializer.Deserialize<PetDto>(content1, _jsonOptions);
            
            firstCallPet.Should().NotBeNull();
            firstCallPet!.Name.Should().Be(originalName);

            // Update the pet through the API (should invalidate cache)
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);
            var newName = $"Updated Pet Name {Guid.NewGuid()}"; // Ensure unique name
            var updatePet = new UpdatePetDto
            {
                Name = newName,
                Description = "Updated description for cache test"
            };
            
            var updateResponse = await client.PutAsJsonAsync($"/api/Pets/{pet.Id}", updatePet);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Get the pet again - should not be from cache but reflect the update
            var response2 = await client.GetAsync($"/api/Pets/{pet.Id}");
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Read the content as a string first to avoid ObjectDisposedException
            var content2 = await response2.Content.ReadAsStringAsync();
            var updatedPet = JsonSerializer.Deserialize<PetDto>(content2, _jsonOptions);
            
            // Verify the pet was updated and cache was invalidated
            updatedPet.Should().NotBeNull();
            updatedPet!.Name.Should().Be(newName);
            updatedPet.Name.Should().NotBe(originalName);
        }

        [Fact]
        public async Task DeletePet_InvalidatesCache()
        {
            // Use a fresh client for this test
            using var client = _factory.CreateClient();
            
            // Get a pet to delete
            var allPetsResponse = await client.GetAsync("/api/Pets");
            allPetsResponse.EnsureSuccessStatusCode();
            
            // Read the content as a string first to avoid ObjectDisposedException
            var allPetsContent = await allPetsResponse.Content.ReadAsStringAsync();
            
            // Try to deserialize as PaginatedResponseDto first, then fall back to IEnumerable if that fails
            IEnumerable<PetDto> allPets;
            try 
            {
                var paginatedResponse = JsonSerializer.Deserialize<PaginatedResponseDto<PetDto>>(allPetsContent, _jsonOptions);
                allPets = paginatedResponse?.Items ?? new List<PetDto>();
            }
            catch
            {
                allPets = JsonSerializer.Deserialize<IEnumerable<PetDto>>(allPetsContent, _jsonOptions) ?? new List<PetDto>();
            }
            
            if (allPets == null || !allPets.Any())
            {
                // Skip test if no pets are available
                return;
            }
            
            var pet = allPets.First();

            // First call to cache the pet
            var response1 = await client.GetAsync($"/api/Pets/{pet.Id}");
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Read the content as a string first to avoid ObjectDisposedException
            var content1 = await response1.Content.ReadAsStringAsync();
            var cachedPet = JsonSerializer.Deserialize<PetDto>(content1, _jsonOptions);
            
            cachedPet.Should().NotBeNull();

            // Delete the pet through the API (should invalidate cache)
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _adminToken);
            var deleteResponse = await client.DeleteAsync($"/api/Pets/{pet.Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify the pet was deleted from the database
            var dbPet = await _dbContext.Pets.FindAsync(pet.Id);
            dbPet.Should().BeNull();

            // Get the pet again - should not be from cache and should return 404
            var response2 = await client.GetAsync($"/api/Pets/{pet.Id}");
            response2.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}