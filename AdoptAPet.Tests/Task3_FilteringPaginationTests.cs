using AdoptAPet.Api.Data;
using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AdoptAPet.Tests
{
    public class Task3_FilteringPaginationTests : ApiTestBase
    {
        public Task3_FilteringPaginationTests() : base()
        {
            SetupTestData().Wait();
        }

        private async Task SetupTestData()
        {
            // Create a test shelter
            var shelter = new Shelter
            {
                Name = "Pagination Test Shelter",
                Address = "123 Pagination St",
                Phone = "555-123-4567",
                Email = "pagination@shelter.com"
            };
            _dbContext.Shelters.Add(shelter);
            await _dbContext.SaveChangesAsync();

            // Create 10 test pets with different types and statuses
            var petTypes = new[] { PetType.Dog, PetType.Cat, PetType.Bird, PetType.Rabbit, PetType.Other };
            var petStatuses = new[] { PetStatus.Available, PetStatus.Pending, PetStatus.Adopted };

            for (int i = 0; i < 10; i++)
            {
                var pet = new Pet
                {
                    Name = $"Pagination Pet {i}",
                    Type = petTypes[i % petTypes.Length],
                    Breed = $"Breed {i}",
                    Age = i + 1,
                    Description = $"Description for pet {i}",
                    Status = petStatuses[i % petStatuses.Length],
                    ShelterId = shelter.Id
                };
                _dbContext.Pets.Add(pet);
            }
            await _dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task GetPets_WithPagination_ReturnsCorrectPage()
        {
            // Act
            var response = await _client.GetAsync("/api/Pets?pageNumber=1&pageSize=5");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponseDto<PetDto>>(_jsonOptions);

            result.Should().NotBeNull();
            result!.Items.Should().NotBeNull();
            result.Items.Should().HaveCount(5);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(5);
            result.TotalCount.Should().BeGreaterThan(5);
            result.TotalPages.Should().BeGreaterThan(1);
        }

        [Fact]
        public async Task GetPets_WithPagination_SecondPage_ReturnsCorrectItems()
        {
            // Act
            var response = await _client.GetAsync("/api/Pets?pageNumber=2&pageSize=5");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponseDto<PetDto>>(_jsonOptions);

            result.Should().NotBeNull();
            result!.Items.Should().NotBeNull();
            result.PageNumber.Should().Be(2);
            result.Items.Should().HaveCount(result.PageSize <= result.TotalCount - 5 ? 5 : result.TotalCount - 5);
        }

        [Fact]
        public async Task GetPets_WithTypeFilter_ReturnsFilteredPets()
        {
            // Act
            var response = await _client.GetAsync("/api/Pets?type=Dog");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponseDto<PetDto>>(_jsonOptions);

            result.Should().NotBeNull();
            result!.Items.Should().NotBeNull();
            result.Items.Should().AllSatisfy(pet => pet.Type.Should().Be(PetType.Dog));
        }

        [Fact]
        public async Task GetPets_WithStatusFilter_ReturnsFilteredPets()
        {
            // Act
            var response = await _client.GetAsync("/api/Pets?status=Available");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponseDto<PetDto>>(_jsonOptions);

            result.Should().NotBeNull();
            result!.Items.Should().NotBeNull();
            result.Items.Should().AllSatisfy(pet => pet.Status.Should().Be(PetStatus.Available));
        }

        [Fact]
        public async Task GetPets_WithMultipleFilters_ReturnsFilteredPets()
        {
            // Act
            var response = await _client.GetAsync("/api/Pets?type=Dog&status=Available");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponseDto<PetDto>>(_jsonOptions);

            result.Should().NotBeNull();
            result!.Items.Should().NotBeNull();
            result.Items.Should().AllSatisfy(pet =>
            {
                pet.Type.Should().Be(PetType.Dog);
                pet.Status.Should().Be(PetStatus.Available);
            });
        }



        [Fact]
        public async Task GetPets_WithFilterAndPagination_ReturnsCorrectItems()
        {
            // Act
            var response = await _client.GetAsync("/api/Pets?type=Dog&pageNumber=1&pageSize=2");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<PaginatedResponseDto<PetDto>>(_jsonOptions);

            result.Should().NotBeNull();
            result!.Items.Should().NotBeNull();
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(2);
            result.Items.Should().AllSatisfy(pet => pet.Type.Should().Be(PetType.Dog));
        }
    }

    // Define a DTO for paginated responses if not already defined
    public class PaginatedResponseDto<T>
    {
        public IEnumerable<T>? Items { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}