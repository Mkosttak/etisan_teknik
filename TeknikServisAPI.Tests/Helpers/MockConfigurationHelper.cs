using Microsoft.Extensions.Configuration;

namespace TeknikServisAPI.Tests.Helpers
{
    /// <summary>
    /// Helper for creating mock IConfiguration instances for testing
    /// Used for services that require configuration (e.g., AuthService needing JwtSettings)
    /// 
    /// Usage:
    ///   var config = MockConfigurationHelper.CreateWithJwtSettings();
    ///   var authService = new AuthService(context, config);
    /// </summary>
    public static class MockConfigurationHelper
    {
        /// <summary>
        /// Creates a mock IConfiguration with JWT settings
        /// Secret key is 32+ characters for HS256 algorithm requirement
        /// </summary>
        /// <returns>IConfiguration with JwtSettings section populated</returns>
        public static IConfiguration CreateWithJwtSettings()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                {"JwtSettings:SecretKey", "ThisIsATestSecretKeyWith32CharactersMinimum!"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"},
                {"JwtSettings:ExpirationHours", "24"}
            };
            
            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }
        
        /// <summary>
        /// Creates a mock IConfiguration with admin bootstrap settings
        /// Includes JWT settings + admin-specific configuration
        /// </summary>
        /// <returns>IConfiguration with JwtSettings and AdminBootstrap sections</returns>
        public static IConfiguration CreateWithAdminBootstrap()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                // JWT Settings
                {"JwtSettings:SecretKey", "ThisIsATestSecretKeyWith32CharactersMinimum!"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"},
                {"JwtSettings:ExpirationHours", "24"},
                
                // Admin Bootstrap
                {"AdminBootstrap:Password", "TestAdmin123!"},
                {"AdminBootstrap:Secret", "TestSecret123"},
                {"AdminBootstrap:Enabled", "true"}
            };
            
            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }
        
        /// <summary>
        /// Creates a completely empty configuration
        /// Useful for testing config-related error handling
        /// </summary>
        /// <returns>Empty IConfiguration</returns>
        public static IConfiguration CreateEmpty()
        {
            return new ConfigurationBuilder().Build();
        }
        
        /// <summary>
        /// Creates a custom configuration from provided dictionary
        /// Flexible method for test-specific configuration needs
        /// </summary>
        /// <param name="settings">Dictionary of configuration key-value pairs</param>
        /// <returns>IConfiguration with custom settings</returns>
        public static IConfiguration CreateCustom(Dictionary<string, string> settings)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings!)
                .Build();
        }
    }
}
