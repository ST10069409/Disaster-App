using Disaster_App.Data;
using Disaster_App.Models;
using Microsoft.EntityFrameworkCore;

namespace Disaster_App.Tests.Helpers
{
    public static class TestHelpers
    {
        /// <summary>
        /// Creates an in-memory database context for testing
        /// </summary>
        public static ApplicationDbContext CreateInMemoryDbContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        /// <summary>
        /// Creates a test user with default values
        /// </summary>
        public static User CreateTestUser(
            string email = "test@example.com",
            string password = "Test123!",
            string role = "User",
            string fullName = "Test User")
        {
            return new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = HashPassword(password),
                Role = role,
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Hashes password using the same method as HomeController
        /// </summary>
        public static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Seeds test data into the database
        /// </summary>
        public static void SeedTestData(ApplicationDbContext context)
        {
            // Add test users
            var user1 = CreateTestUser("user1@test.com", "Password123!", "User", "John Doe");
            var user2 = CreateTestUser("user2@test.com", "Password123!", "Volunteer", "Jane Smith");
            var admin = CreateTestUser("admin@test.com", "Password123!", "Admin", "Admin User");

            context.Users.AddRange(user1, user2, admin);
            context.SaveChanges();

            // Add test volunteer
            var volunteer = new Volunteer
            {
                UserID = user2.UserID,
                Skills = "First Aid, Communication",
                Availability = "Weekends",
                JoinedAt = DateTime.Now
            };
            context.Volunteers.Add(volunteer);
            context.SaveChanges();
        }
    }
}

