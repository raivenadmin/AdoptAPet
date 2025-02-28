using AdoptAPet.Api.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AdoptAPet.Tests
{
    public class Task1_ServiceRegistrationTests : ApiTestBase
    {
        [Fact]
        public void ShelterService_ShouldBeRegisteredInDIContainer()
        {
            // Arrange & Act
            var shelterService = _scope.ServiceProvider.GetService<IShelterService>();

            // Assert
            shelterService.Should().NotBeNull("IShelterService should be registered in the DI container");
        }

        [Fact]
        public void AdoptionApplicationService_ShouldBeRegisteredInDIContainer()
        {
            // Arrange & Act
            var applicationService = _scope.ServiceProvider.GetService<IAdoptionApplicationService>();

            // Assert
            applicationService.Should().NotBeNull("IAdoptionApplicationService should be registered in the DI container");
        }

        [Fact]
        public void PetService_ShouldBeRegisteredInDIContainer()
        {
            // Arrange & Act
            var petService = _scope.ServiceProvider.GetService<IPetService>();

            // Assert
            petService.Should().NotBeNull("IPetService should be registered in the DI container");
        }

        [Fact]
        public void AuthService_ShouldBeRegisteredInDIContainer()
        {
            // Arrange & Act
            var authService = _scope.ServiceProvider.GetService<IAuthService>();

            // Assert
            authService.Should().NotBeNull("IAuthService should be registered in the DI container");
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