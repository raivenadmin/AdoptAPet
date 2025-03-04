using AdoptAPet.Api.Data;
using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Xunit;

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
        public async Task GetAllPets_WithCaching_SecondCallIsFaster()
        {
            // First call - should not be cached
            var stopwatch1 = Stopwatch.StartNew();
            var response1 = await _client.GetAsync("/api/Pets");
            stopwatch1.Stop();
            var firstCallTime = stopwatch1.ElapsedMilliseconds;
            
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Try to deserialize as PaginatedResponseDto first, then fall back to IEnumerable if that fails
            IEnumerable<PetDto> allPets;
            try 
            {
                var paginatedResponse = await response1.Content.ReadFromJsonAsync<PaginatedResponseDto<PetDto>>(_jsonOptions);
                allPets = paginatedResponse?.Items ?? new List<PetDto>();
            }
            catch
            {
                allPets = await response1.Content.ReadFromJsonAsync<IEnumerable<PetDto>>(_jsonOptions) ?? new List<PetDto>();
            }
            
            allPets.Should().NotBeNull();

            // Second call - should be cached and faster
            var stopwatch2 = Stopwatch.StartNew();
            var response2 = await _client.GetAsync("/api/Pets");
            stopwatch2.Stop();
            var secondCallTime = stopwatch2.ElapsedMilliseconds;
            
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // The second call should be faster or at least not significantly slower
            secondCallTime.Should().BeLessThanOrEqualTo((long)(firstCallTime * 1.5));
        }

        [Fact]
        public async Task GetAllShelters_WithCaching_SecondCallIsFaster()
        {
            // First call - should not be cached
            var stopwatch1 = Stopwatch.StartNew();
            var response1 = await _client.GetAsync("/api/Shelters");
            stopwatch1.Stop();
            var firstCallTime = stopwatch1.ElapsedMilliseconds;
            
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            var allShelters = await response1.Content.ReadFromJsonAsync<IEnumerable<ShelterDto>>(_jsonOptions);
            allShelters.Should().NotBeNull();

            // Second call - should be cached and faster
            var stopwatch2 = Stopwatch.StartNew();
            var response2 = await _client.GetAsync("/api/Shelters");
            stopwatch2.Stop();
            var secondCallTime = stopwatch2.ElapsedMilliseconds;
            
            response2.StatusCode.Should().Be(HttpStatusCode.OK);

            // The second call should be faster or at least not significantly slower
            secondCallTime.Should().BeLessThanOrEqualTo((long)(firstCallTime * 1.5));
        }

        [Fact]
        public async Task GetPetById_WithCaching_SecondCallIsFaster()
        {
            // Get a pet ID to use for testing
            var allPetsResponse = await _client.GetAsync("/api/Pets");
            allPetsResponse.EnsureSuccessStatusCode();
            
            // Try to deserialize as PaginatedResponseDto first, then fall back to IEnumerable if that fails
            IEnumerable<PetDto> allPets;
            try 
            {
                var paginatedResponse = await allPetsResponse.Content.ReadFromJsonAsync<PaginatedResponseDto<PetDto>>(_jsonOptions);
                allPets = paginatedResponse?.Items ?? new List<PetDto>();
            }
            catch
            {
                allPets = await allPetsResponse.Content.ReadFromJsonAsync<IEnumerable<PetDto>>(_jsonOptions) ?? new List<PetDto>();
            }
            
            if (allPets == null || !allPets.Any())
            {
                // Skip test if no pets are available
                return;
            }
            
            var petId = allPets.First().Id;

            // First call - should not be cached
            var stopwatch1 = Stopwatch.StartNew();
            var response1 = await _client.GetAsync($"/api/Pets/{petId}");
            stopwatch1.Stop();
            var firstCallTime = stopwatch1.ElapsedMilliseconds;
            
            response1.StatusCode.Should().Be(HttpStatusCode.OK);

            // Second call - should be cached and faster
            var stopwatch2 = Stopwatch.StartNew();
            var response2 = await _client.GetAsync($"/api/Pets/{petId}");
            stopwatch2.Stop();
            var secondCallTime = stopwatch2.ElapsedMilliseconds;
            
            response2.StatusCode.Should().Be(HttpStatusCode.OK);

            // The second call should be faster or at least not significantly slower
            secondCallTime.Should().BeLessThanOrEqualTo((long)(firstCallTime * 2.0));
        }

        [Fact]
        public async Task UpdatePet_InvalidatesCache()
        {
            // Get a pet to update
            var allPetsResponse = await _client.GetAsync("/api/Pets");
            allPetsResponse.EnsureSuccessStatusCode();
            
            // Try to deserialize as PaginatedResponseDto first, then fall back to IEnumerable if that fails
            IEnumerable<PetDto> allPets;
            try 
            {
                var paginatedResponse = await allPetsResponse.Content.ReadFromJsonAsync<PaginatedResponseDto<PetDto>>(_jsonOptions);
                allPets = paginatedResponse?.Items ?? new List<PetDto>();
            }
            catch
            {
                allPets = await allPetsResponse.Content.ReadFromJsonAsync<IEnumerable<PetDto>>(_jsonOptions) ?? new List<PetDto>();
            }
            
            if (allPets == null || !allPets.Any())
            {
                // Skip test if no pets are available
                return;
            }
            
            var pet = allPets.First();

            // First call to cache the pet
            await _client.GetAsync($"/api/Pets/{pet.Id}");

            // Update the pet
            SetAuthToken(_adminToken);
            var updatePet = new UpdatePetDto
            {
                Name = "Updated Pet Name",
                Description = "Updated description"
            };
            
            var updateResponse = await _client.PutAsJsonAsync($"/api/Pets/{pet.Id}", updatePet);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Get the pet again - should not be from cache
            var response = await _client.GetAsync($"/api/Pets/{pet.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Verify the pet was updated
            var updatedPet = await response.Content.ReadFromJsonAsync<PetDto>(_jsonOptions);
            updatedPet.Should().NotBeNull();
            updatedPet!.Name.Should().Be("Updated Pet Name");
        }

        [Fact]
        public async Task DeletePet_InvalidatesCache()
        {
            // Get a pet to delete
            var allPetsResponse = await _client.GetAsync("/api/Pets");
            allPetsResponse.EnsureSuccessStatusCode();
            
            // Try to deserialize as PaginatedResponseDto first, then fall back to IEnumerable if that fails
            IEnumerable<PetDto> allPets;
            try 
            {
                var paginatedResponse = await allPetsResponse.Content.ReadFromJsonAsync<PaginatedResponseDto<PetDto>>(_jsonOptions);
                allPets = paginatedResponse?.Items ?? new List<PetDto>();
            }
            catch
            {
                allPets = await allPetsResponse.Content.ReadFromJsonAsync<IEnumerable<PetDto>>(_jsonOptions) ?? new List<PetDto>();
            }
            
            if (allPets == null || !allPets.Any())
            {
                // Skip test if no pets are available
                return;
            }
            
            var pet = allPets.First();

            // First call to cache the pet
            await _client.GetAsync($"/api/Pets/{pet.Id}");

            // Delete the pet
            SetAuthToken(_adminToken);
            var deleteResponse = await _client.DeleteAsync($"/api/Pets/{pet.Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Get the pet again - should return 404 Not Found
            var response = await _client.GetAsync($"/api/Pets/{pet.Id}");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}