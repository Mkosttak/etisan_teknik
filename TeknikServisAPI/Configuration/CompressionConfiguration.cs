using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

namespace TeknikServisAPI.Configuration
{
    /// <summary>
    /// Response compression configuration for performance optimization
    /// Reduces bandwidth usage by ~70% for JSON responses
    /// </summary>
    public static class CompressionConfiguration
    {
        public static IServiceCollection AddCompressionConfiguration(this IServiceCollection services)
        {
            services.AddResponseCompression(options =>
            {
                // Enable compression for HTTPS (default: disabled for security)
                options.EnableForHttps = true;
                
                // Compression providers
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                
                // MIME types to compress
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
                {
                    "application/json",
                    "application/xml",
                    "text/plain",
                    "text/css",
                    "text/html",
                    "application/javascript",
                    "image/svg+xml"
                });
            });

            // Brotli compression (best compression, modern browsers)
            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest; // Balance: speed vs size
            });

            // Gzip compression (fallback, universal support)
            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            return services;
        }
    }
}
