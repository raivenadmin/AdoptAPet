using AdoptAPet.Api.Data;
using AdoptAPet.Api.DTOs;
using AdoptAPet.Api;
using AdoptAPet.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AdoptAPet.Tests
{
    public class ApiTestBase : IDisposable
    {
        protected readonly WebApplicationFactory<Program> _factory;
        protected readonly HttpClient _client;
        protected readonly JsonSerializerOptions _jsonOptions;
        protected readonly IServiceScope _scope;
        protected readonly AdoptAPetDbContext _dbContext;

        public ApiTestBase()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Remove the app's DbContext registration
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<AdoptAPetDbContext>));

                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

                        // Add DbContext using an in-memory database for testing
                        services.AddDbContext<AdoptAPetDbContext>(options =>
                        {
                            options.UseInMemoryDatabase("InMemoryDbForTesting");
                        }, ServiceLifetime.Singleton); // Change to Singleton to avoid scoped service error

                        // Build the service provider
                        var sp = services.BuildServiceProvider();

                        // Create a scope to obtain a reference to the database context
                        using var scope = sp.CreateScope();
                        var scopedServices = scope.ServiceProvider;
                        var db = scopedServices.GetRequiredService<AdoptAPetDbContext>();

                        // Ensure the database is created
                        db.Database.EnsureCreated();

                        // Seed the database with test data
                        SeedDatabase(db);
                    });
                });

            _client = _factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Create a scope and get the DbContext for use in tests
            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<AdoptAPetDbContext>();
        }

        protected virtual void SeedDatabase(AdoptAPetDbContext context)
        {
            // This method can be overridden in derived classes to add specific test data
        }

        protected Task<string> GetAuthTokenAsync(string email, string password, string roleStr)
        {
            try
            {
                // Convert string role to UserRole enum
                UserRole role;
                if (!Enum.TryParse(roleStr, out role))
                {
                    role = UserRole.Adopter; // Default to Adopter if parsing fails
                }

                // Instead of using the API, generate a token directly
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email.Split('@')[0]),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, role.ToString())
                };

                // Use a key that's long enough (at least 64 bytes for HMAC-SHA512)
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    "testsecretkeytestsecretkeytestsecretkeytestsecretkeytestsecretkeytestsecretkey"));

                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); // Use SHA256 instead of SHA512

                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds);

                var jwt = new JwtSecurityTokenHandler().WriteToken(token);
                
                // Set the token in the client
                SetAuthToken(jwt);
                
                return Task.FromResult(jwt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating token: {ex.Message}");
                throw;
            }
        }

        protected void SetAuthToken(string token)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public void Dispose()
        {
            _scope.Dispose();
            _client.Dispose();
            _factory.Dispose();
        }
    }

    public class AuthResponseDto
    {
        public string? Token { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }
}