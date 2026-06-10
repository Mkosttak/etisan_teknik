using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Models;
using TeknikServisAPI.Constants;

namespace TeknikServisAPI.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Ensure database is created - this might be redundant if migrations are used, but keeps it safe for dev
            // await context.Database.EnsureCreatedAsync(); 

            // Only seed in Development or if explicitly enabled
            var isDev = app.Environment.IsDevelopment();
            var seedEnabled = configuration.GetValue<bool>("AdminBootstrap:SeedEnabled");

            if (app.Environment.IsProduction())
            {
                return;
            }

            if (!isDev && !seedEnabled)
            {
                return;
            }

            if (!await context.Users.AnyAsync())
            {
                var adminPassword = configuration["AdminBootstrap:Password"];
                if (string.IsNullOrEmpty(adminPassword))
                {
                    return;
                }
                
                var admin = new User
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@etisan.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Role = AppRoles.Admin,
                    PhoneNumber = "5555555555",
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) // Deterministic date
                };

                context.Users.Add(admin);
                await context.SaveChangesAsync();
            }
        }
    }
}
