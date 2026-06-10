using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace TeknikServisAPI.Middleware
{
    /// <summary>
    /// OWASP recommended security headers middleware
    /// Protects against: XSS, Clickjacking, MIME-sniffing, etc.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // X-Content-Type-Options: Prevents MIME-sniffing
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // X-Frame-Options: Prevents clickjacking
            context.Response.Headers["X-Frame-Options"] = "DENY";

            // X-XSS-Protection: Legacy XSS filter (backup for old browsers)
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

            // Referrer-Policy: Controls referrer information
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Content-Security-Policy: Prevents XSS and other code injection
            context.Response.Headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self' 'unsafe-inline'; " +  // unsafe-inline for Ant Design
                "img-src 'self' data: https:; " +
                "font-src 'self' data:; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'";

            // Permissions-Policy: Restricts browser features
            context.Response.Headers["Permissions-Policy"] =
                "geolocation=(), microphone=(), camera=()";

            // HSTS: Force HTTPS (only in production with HTTPS)
            if (context.Request.IsHttps)
            {
                context.Response.Headers["Strict-Transport-Security"] =
                    "max-age=31536000; includeSubDomains; preload";
            }

            // Remove server version disclosure
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await _next(context);
        }
    }

    /// <summary>
    /// Extension method for easy middleware registration
    /// </summary>
    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
