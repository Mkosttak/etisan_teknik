using Serilog.Context;

namespace TeknikServisAPI.Middleware
{
    /// <summary>
    /// Correlation ID Middleware - Distributed tracing ve log correlation için
    /// Her request'e benzersiz bir ID atar ve tüm loglarda kullanılmasını sağlar
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-ID";
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Gelen request'te correlation ID varsa kullan, yoksa yeni oluştur
            var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                               ?? context.TraceIdentifier
                               ?? Guid.NewGuid().ToString("N");
            
            // Response header'ına ekle (client takip edebilsin)
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[CorrelationIdHeader] = correlationId;
                return Task.CompletedTask;
            });
            
            // HttpContext.Items'a ekle (controller'larda kullanılabilir)
            context.Items["CorrelationId"] = correlationId;
            
            // Serilog LogContext'e ekle (tüm loglarda görünür)
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                try
                {
                    await _next(context);
                }
                finally
                {
                    stopwatch.Stop();
                    
                    // Request tamamlandığında duration logla
                    var level = context.Response.StatusCode >= 500 
                        ? Serilog.Events.LogEventLevel.Error 
                        : context.Response.StatusCode >= 400 
                            ? Serilog.Events.LogEventLevel.Warning 
                            : Serilog.Events.LogEventLevel.Information;
                    
                    _logger.Log(
                        (Microsoft.Extensions.Logging.LogLevel)level,
                        "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:0.0000}ms",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        stopwatch.Elapsed.TotalMilliseconds);
                }
            }
        }
    }

    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}
