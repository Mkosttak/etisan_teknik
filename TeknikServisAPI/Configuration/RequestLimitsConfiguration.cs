using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Features;

namespace TeknikServisAPI.Configuration
{
    /// <summary>
    /// Request size limits and timeout configuration
    /// Protects against DoS attacks and large payload abuse
    /// </summary>
    public static class RequestLimitsConfiguration
    {
        public static IServiceCollection AddRequestLimitsConfiguration(this IServiceCollection services)
        {
            services.Configure<KestrelServerOptions>(options =>
            {
                // Maximum request body size: 30 MB (default: 30 MB)
                // Adjust based on your file upload requirements
                options.Limits.MaxRequestBodySize = 30 * 1024 * 1024; // 30 MB
                
                // Maximum request header size: 32 KB (default: 32 KB)
                options.Limits.MaxRequestHeadersTotalSize = 32 * 1024;
                
                // Maximum number of headers: 100 (default: 100)
                options.Limits.MaxRequestHeaderCount = 100;
                
                // Request timeout: 2 minutes
                options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
                
                // Request drain timeout: 5 seconds
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
            });

            // Form options for file uploads
            services.Configure<FormOptions>(options =>
            {
                // Maximum form body size: 30 MB
                options.MultipartBodyLengthLimit = 30 * 1024 * 1024;
                
                // Maximum form key length
                options.KeyLengthLimit = 2048;
                
                // Maximum form value length
                options.ValueLengthLimit = 4 * 1024 * 1024; // 4 MB
                
                // Buffer threshold for form processing
                options.BufferBodyLengthLimit = 30 * 1024 * 1024;
                
                // Memory buffer size
                options.MemoryBufferThreshold = 128 * 1024; // 128 KB
            });

            return services;
        }
    }
}
