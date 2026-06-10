using Microsoft.Extensions.Hosting;
using System.Net;
using System.Text.Json;
using TeknikServisAPI.Exceptions;

namespace TeknikServisAPI.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // ? KVKK/GDPR: Hassas verileri loglamadan önce temizle
                var sanitizedException = _environment.IsProduction() 
                    ? Utilities.SensitiveDataFilter.SanitizeException(ex)
                    : ex.ToString();
                
                _logger.LogError("Unhandled exception: {Error}", sanitizedException);
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // ? RFC 7807 ProblemDetails standardý - API error response standardizasyonu
            context.Response.ContentType = "application/problem+json";
            var traceId = context.TraceIdentifier;

            var response = new ProblemDetailsResponse
            {
                Type = "https://httpstatuses.com/500", // Will be updated based on exception
                Title = "Bir hata oluþtu",
                Status = 500,
                Detail = "Beklenmeyen bir hata oluþtu",
                Instance = context.Request.Path,
                TraceId = traceId,
                Errors = new List<string>()
            };

            switch (exception)
            {
                case NotFoundException notFoundEx:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Status = 404;
                    response.Type = "https://httpstatuses.com/404";
                    response.Title = "Kayýt bulunamadý";
                    response.Detail = notFoundEx.Message;
                    response.Errors.Add(notFoundEx.Message);
                    break;

                case BusinessValidationException validationEx:
                    context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                    response.Status = 422;
                    response.Type = "https://httpstatuses.com/422";
                    response.Title = "Doðrulama hatasý";
                    response.Detail = validationEx.Message;
                    response.Errors.Add(validationEx.Message);
                    break;
                case ArgumentException argEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Status = 400;
                    response.Type = "https://httpstatuses.com/400";
                    response.Title = "Geçersiz istek";
                    response.Detail = argEx.Message;
                    response.Errors.Add(argEx.Message);
                    break;

                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Status = 401;
                    response.Type = "https://httpstatuses.com/401";
                    response.Title = "Yetkisiz eriþim";
                    response.Detail = "Bu iþlem için yetkiniz yok";
                    break;

                case KeyNotFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Status = 404;
                    response.Type = "https://httpstatuses.com/404";
                    response.Title = "Kayýt bulunamadý";
                    response.Detail = "Ýstenen kayýt veritabanýnda bulunamadý";
                    break;

                case InvalidOperationException invEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Status = 400;
                    response.Type = "https://httpstatuses.com/400";
                    response.Title = "Geçersiz iþlem";
                    response.Detail = invEx.Message;
                    response.Errors.Add(invEx.Message);
                    break;
                
                // ? Database exceptions - MySQL specific
                case Microsoft.EntityFrameworkCore.DbUpdateException dbEx when dbEx.InnerException is MySqlConnector.MySqlException mysqlEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Status = 400;
                    response.Type = "https://httpstatuses.com/400";
                    
                    // MySQL error codes
                    switch (mysqlEx.Number)
                    {
                        case 1062: // Duplicate entry
                            response.Title = "Kayýt zaten mevcut";
                            response.Detail = "Bu kayýt zaten mevcut";
                            response.Errors.Add("Duplicate key constraint violation");
                            break;
                        case 1451: // Foreign key constraint
                            response.Title = "Silme iþlemi engellendi";
                            response.Detail = "Bu kayýt baþka kayýtlar tarafýndan kullanýlýyor, silinemez";
                            response.Errors.Add("Foreign key constraint violation");
                            break;
                        case 1452: // Cannot add or update - foreign key fails
                            response.Title = "Ýliþkili kayýt bulunamadý";
                            response.Detail = "Referans edilen kayýt veritabanýnda bulunamadý";
                            response.Errors.Add("Referenced record not found");
                            break;
                        default:
                            response.Title = "Veritabaný hatasý";
                            response.Detail = "Veritabaný iþlemi baþarýsýz";
                            if (_environment.IsDevelopment())
                                response.Errors.Add(mysqlEx.Message);
                            break;
                    }
                    break;
                
                // Concurrency conflict
                case Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException concurrencyEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    response.Status = 409;
                    response.Type = "https://httpstatuses.com/409";
                    response.Title = "Eþzamanlýlýk çakýþmasý";
                    response.Detail = "Bu kayýt baþka bir kullanýcý tarafýndan deðiþtirildi. Lütfen sayfayý yenileyip tekrar deneyin.";
                    response.Errors.Add("Concurrency conflict");
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Status = 500;
                    response.Type = "https://httpstatuses.com/500";
                    response.Title = "Sunucu hatasý";
                    response.Detail = "Sunucu hatasý, lütfen daha sonra tekrar deneyin";
                    if (_environment.IsDevelopment())
                    {
                        response.Errors.Add(exception.Message);
                    }
                    break;
            }

            var result = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return context.Response.WriteAsync(result);
        }
    }

    /// <summary>
    /// RFC 7807 ProblemDetails uyumlu error response
    /// </summary>
    public class ProblemDetailsResponse
    {
        public string Type { get; set; } = "https://httpstatuses.com/500";
        public string Title { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }
}
