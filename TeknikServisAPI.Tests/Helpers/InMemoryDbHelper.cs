using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Data;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Tests.Helpers
{
    /// <summary>
    /// Helper for creating InMemory database contexts for testing
    /// Ensures each test has an isolated database to prevent test interference
    /// 
    /// Usage:
    ///   using var context = InMemoryDbHelper.CreateContext("UniqueTestName");
    ///   InMemoryDbHelper.SeedTestData(context);
    /// </summary>
    public static class InMemoryDbHelper
    {
        /// <summary>
        /// Creates a new InMemory ApplicationDbContext with unique database name
        /// Each test should use a different database name for isolation
        /// </summary>
        /// <param name="databaseName">Unique name for this test database. If null, generates GUID.</param>
        /// <returns>Configured ApplicationDbContext ready for testing</returns>
        public static ApplicationDbContext CreateContext(string? databaseName = null)
        {
            // Use GUID if no name provided to ensure uniqueness
            databaseName ??= Guid.NewGuid().ToString();
            
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName)
                .EnableSensitiveDataLogging()
                .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            
            return new ApplicationDbContext(options);
        }
        
        /// <summary>
        /// Seeds common test data into the context
        /// Creates: 2 users (regular + admin), 1 institution, 1 stock item
        /// </summary>
        /// <param name="context">The context to seed</param>
        public static void SeedTestData(ApplicationDbContext context)
        {
            // Test users with known passwords
            context.Users.AddRange(
                new User 
                { 
                    Id = 1,
                    Email = "test@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPassword123!"),
                    FirstName = "Test",
                    LastName = "User",
                    Role = "Teknisyen",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 2,
                    Email = "admin@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPassword123!"),
                    FirstName = "Admin",
                    LastName = "User",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
            
            // Test institution
            context.Institutions.Add(new Institution
            {
                Id = 1,
                Name = "Test Kurum",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            
            // Test stock item
            context.StockItems.Add(new StockItem
            {
                Id = 1,
                ItemName = "Test Malzeme",
                CurrentStock = 100,
                MinimumStock = 10,
                Unit = "Adet",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            
            context.SaveChanges();
        }
        
        /// <summary>
        /// Creates a context and seeds it with test data in one call
        /// Convenience method for common scenario
        /// </summary>
        /// <param name="databaseName">Unique database name</param>
        /// <returns>Seeded context ready for testing</returns>
        public static ApplicationDbContext CreateSeededContext(string? databaseName = null)
        {
            var context = CreateContext(databaseName);
            SeedTestData(context);
            return context;
        }
    }
}
