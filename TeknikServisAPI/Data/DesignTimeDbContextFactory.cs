using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TeknikServisAPI.Data
{
    /// <summary>
    /// EF Core migration'ları için design-time factory.
    /// Bu sayede 'dotnet ef' komutları Program.cs'i çalıştırmaz,
    /// HostAbortedException hatası önlenir.
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // appsettings.json ve User Secrets'tan konfigürasyonu oku
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddUserSecrets<DesignTimeDbContextFactory>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // GÜVENLIK: Fallback connection string KALDIRILDI
            // SADECE User Secrets veya Environment Variables kullanılmalıdır
            if (string.IsNullOrEmpty(connectionString) || connectionString == "TO_BE_SET_IN_USER_SECRETS_OR_ENV")
            {
                throw new InvalidOperationException(
                    "Connection string bulunamadı. Lütfen User Secrets veya environment variable ile yapılandırın:\n" +
                    "  dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"your-connection-string\"\n" +
                    "veya\n" +
                    "  set ConnectionStrings__DefaultConnection=your-connection-string"
                );
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
