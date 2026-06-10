using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TeknikServisAPI.Constants;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Services;
using Asp.Versioning;

namespace TeknikServisAPI.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Route("api/[controller]")] // Geriye uyumluluk için
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IWebHostEnvironment _environment;

        public AuthController(IAuthService authService, IConfiguration configuration, IWebHostEnvironment environment, ILogger<AuthController> logger)
        {
            _authService = authService;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Legacy login endpoint - geriye uyumluluk için
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            
            if (result == null)
            {
                return Unauthorized(new { message = "Email veya þifre hatalý" });
            }

            return Ok(result);
        }
        
        /// <summary>
        /// Refresh token destekli login - Production için önerilen
        /// </summary>
        [HttpPost("login-v2")]
        public async Task<IActionResult> LoginWithRefresh([FromBody] LoginRequestDto loginDto)
        {
            var ipAddress = GetIpAddress();
            var result = await _authService.LoginWithRefreshAsync(loginDto, ipAddress);
            
            if (result == null)
            {
                return Unauthorized(new { message = "Email veya þifre hatalý" });
            }

            return Ok(result);
        }
        
        /// <summary>
        /// Refresh token ile yeni access token al
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var ipAddress = GetIpAddress();
            var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);
            
            if (result == null)
            {
                return Unauthorized(new { message = "Geçersiz veya süresi dolmuþ refresh token" });
            }

            return Ok(result);
        }
        
        /// <summary>
        /// Logout - Refresh token'ý iptal et
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto? request)
        {
            // Refresh token verilmiþse onu iptal et
            if (request != null && !string.IsNullOrEmpty(request.RefreshToken))
            {
                var ipAddress = GetIpAddress();
                await _authService.RevokeTokenAsync(request.RefreshToken, ipAddress);
            }
            
            return Ok(new { message = "Çýkýþ baþarýlý" });
        }
        
        private string? GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                return Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            }
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            // Email claim'ini al
            var email = User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                       ?? User?.FindFirst("email")?.Value;
            
            // Eðer email claim'i yoksa, Subject (user ID) kullan
            if (string.IsNullOrEmpty(email))
            {
                var userIdClaim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                ?? User?.FindFirst("sub")?.Value;
                
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    var userById = await _authService.GetUserByIdAsync(userId);
                    if (userById != null)
                    {
                        return Ok(new UserDto
                        {
                            Id = userById.Id,
                            FirstName = userById.FirstName,
                            LastName = userById.LastName,
                            Email = userById.Email,
                            Role = userById.Role,
                            PhoneNumber = userById.PhoneNumber,
                            IsActive = userById.IsActive,
                            CreatedAt = userById.CreatedAt,
                            LastLoginAt = userById.LastLoginAt
                        });
                    }
                }
                
                return Unauthorized(new { message = "Geçersiz kullanýcý" });
            }

            // Email'den kullanýcýyý bul
            var user = await _authService.GetUserByEmailAsync(email);
            
            if (user == null)
            {
                return NotFound(new { message = "Kullanýcý bulunamadý" });
            }

            return Ok(new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }

        [HttpPost("create-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateAdmin()
        {
            if (_environment.IsProduction())
            {
                return Forbid("Admin creation is disabled in production.");
            }

            // 1. Check if bootstrapping is enabled
            var isEnabled = _configuration.GetValue<bool>("AdminBootstrap:Enabled");
            if (!isEnabled)
            {
                return Forbid("Admin creation is disabled.");
            }

            // 2. Check secret header
            var expectedSecret = _configuration["AdminBootstrap:Secret"];
            if (string.IsNullOrEmpty(expectedSecret))
            {
                return Forbid("Admin creation secret is not configured.");
            }

            if (!Request.Headers.TryGetValue("X-Admin-Secret", out var providedSecret) || providedSecret != expectedSecret)
            {
                return Unauthorized("Invalid admin secret.");
            }

            try
            {
                var result = await _authService.CreateAdminUserAsync();
                
                if (result == null)
                {
                    return Ok(new { message = "Admin kullanýcýsý zaten mevcut", exists = true });
                }

                return Ok(new { message = "Admin kullanýcýsý baþarýyla oluþturuldu", exists = false, user = new { id = result.Id, email = result.Email, firstName = result.FirstName, lastName = result.LastName } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin oluþturma hatasý");
                return StatusCode(500, new { message = "Admin oluþturulurken hata oluþtu" });
            }
        }

        [HttpGet("users")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            var userDtos = users.Select(user => new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            }).ToList();
            
            return Ok(userDtos);
        }

        [HttpGet("users/{id}")]
        [Authorize]
        public async Task<IActionResult> GetUserById(int id)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole(AppRoles.Admin);
            if (!isAdmin && currentUserId != id.ToString())
            {
                return Forbid();
            }
            var user = await _authService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Kullanýcý bulunamadý" });
            }
            
            return Ok(new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }

        [HttpPost("users")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            var result = await _authService.CreateUserAsync(createUserDto);
            
            if (result == null)
            {
                return BadRequest(new { message = "Bu email adresi zaten kullanýlýyor" });
            }

            var userDto = new UserDto
            {
                Id = result.Id,
                FirstName = result.FirstName,
                LastName = result.LastName,
                Email = result.Email,
                Role = result.Role,
                PhoneNumber = result.PhoneNumber,
                IsActive = result.IsActive,
                CreatedAt = result.CreatedAt,
                LastLoginAt = result.LastLoginAt
            };

            return CreatedAtAction(nameof(GetUserById), new { id = result.Id }, userDto);
        }

        [HttpPut("users/{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            var result = await _authService.UpdateUserAsync(id, updateUserDto);
            
            if (result == null)
            {
                return NotFound(new { message = "Kullanýcý bulunamadý" });
            }

            return Ok(new UserDto
            {
                Id = result.Id,
                FirstName = result.FirstName,
                LastName = result.LastName,
                Email = result.Email,
                Role = result.Role,
                PhoneNumber = result.PhoneNumber,
                IsActive = result.IsActive,
                CreatedAt = result.CreatedAt,
                LastLoginAt = result.LastLoginAt
            });
        }

        [HttpDelete("users/{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _authService.DeleteUserAsync(id);
            
            if (!result)
            {
                return NotFound(new { message = "Kullanýcý bulunamadý" });
            }

            return Ok(new { message = "Kullanýcý baþarýyla silindi" });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdValue) || !int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { message = "Kullanýcý bilgisi bulunamadý" });
            }

            if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest(new { message = "Mevcut ve yeni þifre zorunludur" });
            }

            if (dto.NewPassword.Length < 8)
            {
                return BadRequest(new { message = "Yeni þifre en az 8 karakter olmalý" });
            }

            if (dto.CurrentPassword == dto.NewPassword)
            {
                return BadRequest(new { message = "Yeni þifre mevcut þifre ile ayný olamaz" });
            }

            var updated = await _authService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
            if (!updated)
            {
                return BadRequest(new { message = "Mevcut þifre hatalý veya kullanýcý bulunamadý" });
            }

            return Ok(new { message = "Þifre baþarýyla güncellendi" });
        }
    }
}
