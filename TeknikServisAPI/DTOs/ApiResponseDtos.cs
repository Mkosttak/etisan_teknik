namespace TeknikServisAPI.DTOs
{
    /// <summary>
    /// Standart API hata response modeli
    /// Tüm hata yanıtları için tutarlı format sağlar
    /// </summary>
    public class ApiErrorResponse
    {
        public bool Success => false;
        
        /// <summary>
        /// Kullanıcı dostu hata mesajı
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Programatik hata kodu (opsiyonel)
        /// Örnek: "USER_NOT_FOUND", "VALIDATION_ERROR", "UNAUTHORIZED"
        /// </summary>
        public string? ErrorCode { get; set; }
        
        /// <summary>
        /// Validation hataları (FluentValidation için)
        /// Key: alan adı, Value: hata mesajları
        /// </summary>
        public Dictionary<string, string[]>? ValidationErrors { get; set; }
        
        /// <summary>
        /// Correlation ID (distributed tracing için)
        /// </summary>
        public string? CorrelationId { get; set; }
        
        /// <summary>
        /// Hata zamanı (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Factory methods
        public static ApiErrorResponse NotFound(string message, string? errorCode = null) => new()
        {
            Message = message,
            ErrorCode = errorCode ?? "NOT_FOUND"
        };
        
        public static ApiErrorResponse BadRequest(string message, string? errorCode = null) => new()
        {
            Message = message,
            ErrorCode = errorCode ?? "BAD_REQUEST"
        };
        
        public static ApiErrorResponse Unauthorized(string message = "Kimlik doğrulama gerekli") => new()
        {
            Message = message,
            ErrorCode = "UNAUTHORIZED"
        };
        
        public static ApiErrorResponse Forbidden(string message = "Bu işlem için yetkiniz yok") => new()
        {
            Message = message,
            ErrorCode = "FORBIDDEN"
        };
        
        public static ApiErrorResponse Conflict(string message, string? errorCode = null) => new()
        {
            Message = message,
            ErrorCode = errorCode ?? "CONFLICT"
        };
        
        public static ApiErrorResponse InternalError(string message = "Bir hata oluştu") => new()
        {
            Message = message,
            ErrorCode = "INTERNAL_ERROR"
        };
        
        public static ApiErrorResponse Validation(Dictionary<string, string[]> errors) => new()
        {
            Message = "Doğrulama hatası",
            ErrorCode = "VALIDATION_ERROR",
            ValidationErrors = errors
        };
    }
    
    /// <summary>
    /// Standart API başarı response modeli
    /// </summary>
    public class ApiSuccessResponse<T>
    {
        public bool Success => true;
        public T? Data { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public static ApiSuccessResponse<T> Ok(T data, string? message = null) => new()
        {
            Data = data,
            Message = message
        };
    }
}
