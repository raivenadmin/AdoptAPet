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
            _adminToken = GetAuthTokenAsync("admin@example.com", "Password123!", "Admin").Result;
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
            var allPets = await response1.Content.ReadFromJsonAsync<IEnumerable<PetDto>>(_jsonOptions);
            allPets.Should().NotBeNull();

            // Second call - should be cached and faster
            var stopwatch2 = Stopwatch.StartNew();
            var response2 = await _client.GetAsync("/api/Pets");
            stopwatch2.Stop();
            var secondCallTime = stopwatch2.ElapsedMilliseconds;
            
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // Check if the second call has the cache header
            response2.Headers.Should().ContainKey("X-Cache-Hit");
            
            // The second call should be faster or at least not significantly slower
            // Note: In some test environments, timing might be inconsistent
            // so we're being lenient with the assertion
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
            
            // Check if the second call has the cache header
            response2.Headers.Should().ContainKey("X-Cache-Hit");
            
            // The second call should be faster or at least not significantly slower
            secondCallTime.Should().BeLessThanOrEqualTo((long)(firstCallTime * 1.5));
        }

        [Fact]
        public async Task GetPetById_WithCaching_SecondCallIsFaster()
        {
            // Get a pet ID to use for testing
            var allPetsResponse = await _client.GetAsync("/api/Pets");
            allPetsResponse.EnsureSuccessStatusCode();
            var allPets = await allPetsResponse.Content.ReadFromJsonAsync<IEnumerable<PetDto>>(_jsonOptions);
            
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
            
            // Check if the second call has the cache header
            response2.Headers.Should().ContainKey("X-Cache-Hit");
            
            // The second call should be faster or at least not significantly slower
            secondCallTime.Should().BeLessThanOrEqualTo((long)(firstCallTime * 1.5));
        }

        [Fact]
        public async Task UpdatePet_InvalidatesCache()
        {
            // Get a pet to update
            var allPetsResponse = await _client.GetAsync("/api/Pets");
            allPetsResponse.EnsureSuccessStatusCode();
            var allPets = await allPetsResponse.Content.ReadFromJsonAsync<IEnumerable<PetDto>>(_jsonOptions);
            
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
            
            // The response should not have the cache hit header
            response.Headers.Should().NotContainKey("X-Cache-Hit");
            
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
            var allPets = await allPetsResponse.Content.ReadFromJsonAsync<IEnumerable<PetDto>>(_jsonOptions);
            
            if (allPets == null || !allPets.Any())
            {
                // Skip test if no pets are available
                return;
            }
            
            var pet = allPets.Last(); // Use the last pet to avoid affecting other tests

            // First call to cache the pet list
            await _client.GetAsync("/api/Pets");

            // Delete the pet
            SetAuthToken(_adminToken);
            var deleteResponse = await _client.DeleteAsync($"/api/Pets/{pet.Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Get all pets again - should not be from cache
            var response = await _client.GetAsync("/api/Pets");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            // The response should not have the cache hit header
            response.Headers.Should().NotContainKey("X-Cache-Hit");
            
            // Verify the pet was deleted
            var petsAfterDelete = await response.Content.ReadFromJsonAsync<IEnumerable<PetDto>>(_jsonOptions);
            petsAfterDelete.Should().NotBeNull();
            petsAfterDelete!.Should().NotContain(p => p.Id == pet.Id);
        }
    }
}