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
        protected User? _testUser;

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
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            // Create a scope and get the DbContext for use in tests
            _scope = _factory.Services.CreateScope();
            _dbContext = _scope.ServiceProvider.GetRequiredService<AdoptAPetDbContext>();
        }

        protected virtual void SeedDatabase(AdoptAPetDbContext context)
        {
            // Create test users if they don't exist
            if (!context.Users.Any())
            {
                var adminUser = new User
                {
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = Encoding.UTF8.GetBytes("hashed_password_123"),
                    PasswordSalt = Encoding.UTF8.GetBytes("salt_123"),
                    Role = UserRole.Admin
                };
                
                var staffUser = new User
                {
                    Username = "staff",
                    Email = "staff@example.com",
                    PasswordHash = Encoding.UTF8.GetBytes("hashed_password_123"),
                    PasswordSalt = Encoding.UTF8.GetBytes("salt_123"),
                    Role = UserRole.ShelterStaff
                };
                
                var adopterUser = new User
                {
                    Username = "adopter",
                    Email = "adopter@example.com",
                    PasswordHash = Encoding.UTF8.GetBytes("hashed_password_123"),
                    PasswordSalt = Encoding.UTF8.GetBytes("salt_123"),
                    Role = UserRole.Adopter
                };
                
                context.Users.Add(adminUser);
                context.Users.Add(staffUser);
                context.Users.Add(adopterUser);
                context.SaveChanges();
                
                _testUser = adopterUser;
            }
            else
            {
                _testUser = context.Users.FirstOrDefault(u => u.Role == UserRole.Adopter);
            }
        }

        protected async Task<string> GetAuthTokenAsync(UserRole role)
        {
            try
            {
                string email = role switch
                {
                    UserRole.Admin => "admin@example.com",
                    UserRole.ShelterStaff => "staff@example.com",
                    UserRole.Adopter => "adopter@example.com",
                    _ => throw new ArgumentException($"Invalid role: {role}")
                };

                // Find or create the user
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    // Create a new user with the specified role
                    user = new User
                    {
                        Username = email.Split('@')[0],
                        Email = email,
                        PasswordHash = Encoding.UTF8.GetBytes("hashed_password_123"),
                        PasswordSalt = Encoding.UTF8.GetBytes("salt_123"),
                        Role = role
                    };

                    // Assign shelter ID for shelter staff
                    if (role == UserRole.ShelterStaff)
                    {
                        var shelter = await _dbContext.Shelters.FirstOrDefaultAsync();
                        if (shelter != null)
                        {
                            user.ShelterId = shelter.Id;
                        }
                    }

                    _dbContext.Users.Add(user);
                    await _dbContext.SaveChangesAsync();
                }

                int userId = user.Id;

                // Create claims for the JWT token
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, role.ToString())
                };

                // Add ShelterId claim for ShelterStaff
                if (role == UserRole.ShelterStaff && user.ShelterId.HasValue)
                {
                    claims.Add(new Claim("ShelterId", user.ShelterId.Value.ToString()));
                }

                // Use the exact same key as in Program.cs
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("super_secret_key_for_jwt_token_generation_12345678901234567890"));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds);

                var jwt = new JwtSecurityTokenHandler().WriteToken(token);
                
                // Set the token in the client
                SetAuthToken(jwt);
                
                return jwt;
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
            // Dispose in the correct order to prevent ObjectDisposedException
            _client.DefaultRequestHeaders.Clear();
            _client.Dispose();
            _scope.Dispose();
            _factory.Dispose();
            GC.SuppressFinalize(this);
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