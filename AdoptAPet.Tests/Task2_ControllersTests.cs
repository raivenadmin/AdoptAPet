using AdoptAPet.Api.DTOs;
using AdoptAPet.Api.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

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
            _adminToken = GetAuthTokenAsync("admin@example.com", "Password123!", "Admin").Result;
            _shelterStaffToken = GetAuthTokenAsync("staff@example.com", "Password123!", "ShelterStaff").Result;
            _adopterToken = GetAuthTokenAsync("adopter@example.com", "Password123!", "Adopter").Result;
        }

        [Fact]
        public async Task SheltersController_GetAll_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/Shelters");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var shelters = await response.Content.ReadFromJsonAsync<IEnumerable<ShelterDto>>(_jsonOptions);
            shelters.Should().NotBeNull();
        }

        [Fact]
        public async Task SheltersController_GetById_ReturnsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/Shelters/1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var shelter = await response.Content.ReadFromJsonAsync<ShelterDto>(_jsonOptions);
            shelter.Should().NotBeNull();
        }

        [Fact]
        public async Task SheltersController_Create_AdminCanCreate()
        {
            // Arrange
            SetAuthToken(_adminToken);
            var newShelter = new CreateShelterDto
            {
                Name = "New Test Shelter",
                Address = "123 Test St",
                Phone = "555-123-4567",
                Email = "newtest@shelter.com"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Shelters", newShelter);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task SheltersController_Create_AdopterCannotCreate()
        {
            // Arrange
            SetAuthToken(_adopterToken);
            var newShelter = new CreateShelterDto
            {
                Name = "Unauthorized Shelter",
                Address = "123 Test St",
                Phone = "555-123-4567",
                Email = "unauth@shelter.com"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Shelters", newShelter);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
            var newApplication = new CreateAdoptionApplicationDto
            {
                PetId = 1,
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
        public async Task AdoptionApplicationsController_Approve_AdminCanApprove()
        {
            // Arrange
            SetAuthToken(_adminToken);

            // Act
            var response = await _client.PutAsync("/api/AdoptionApplications/1/approve", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var application = await response.Content.ReadFromJsonAsync<AdoptionApplicationDto>(_jsonOptions);
            application.Should().NotBeNull();
            application!.Status.Should().Be(ApplicationStatus.Approved);
        }

        [Fact]
        public async Task AdoptionApplicationsController_Approve_AdopterCannotApprove()
        {
            // Arrange
            SetAuthToken(_adopterToken);

            // Act
            var response = await _client.PutAsync("/api/AdoptionApplications/1/approve", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
} 