using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;

using TeknikServisAPI.Constants;
namespace TeknikServisAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return null;
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return new LoginResponseDto
            {
                Token = token,
                User = new UserDto
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
                }
            };
        }

        public string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expirationHours = int.Parse(jwtSettings["ExpirationHours"]!);

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Added for Controller User.Identity.Name / FindFirst(ClaimTypes.NameIdentifier) support
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expirationHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> CreateUserAsync(CreateUserDto createUserDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
            {
                return null;
            }
            
            // ⭐ YENİ: Production-grade şifre güvenlik kontrolü
            var (isValid, errors) = Validators.PasswordValidator.Validate(createUserDto.Password);
            if (!isValid)
            {
                throw new ArgumentException(Validators.PasswordValidator.GetUserFriendlyErrorMessage(errors));
            }

            var user = new User
            {
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                Email = createUserDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                Role = createUserDto.Role,
                PhoneNumber = createUserDto.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User?> UpdateUserAsync(int userId, UpdateUserDto updateUserDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            if (!string.IsNullOrEmpty(updateUserDto.FirstName))
                user.FirstName = updateUserDto.FirstName;

            if (!string.IsNullOrEmpty(updateUserDto.LastName))
                user.LastName = updateUserDto.LastName;

            if (!string.IsNullOrEmpty(updateUserDto.PhoneNumber))
                user.PhoneNumber = updateUserDto.PhoneNumber;

            if (!string.IsNullOrEmpty(updateUserDto.Role))
                user.Role = updateUserDto.Role;

            if (updateUserDto.IsActive.HasValue)
                user.IsActive = updateUserDto.IsActive.Value;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User?> CreateAdminUserAsync()
        {
            if (await _context.Users.AnyAsync(u => u.Email == "admin@etisan.com"))
            {
                return null;
            }

            var adminPassword = _configuration["AdminBootstrap:Password"];
            if (string.IsNullOrEmpty(adminPassword))
            {
                throw new InvalidOperationException("Admin bootstrap password is not configured.");
            }
            
            var adminUser = new User
            {
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@etisan.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = AppRoles.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            return adminUser;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                return false;
            }
            
            // ⭐ YENİ: Yeni şifre güvenlik kontrolü
            var (isValid, errors) = Validators.PasswordValidator.Validate(newPassword);
            if (!isValid)
            {
                throw new ArgumentException(Validators.PasswordValidator.GetUserFriendlyErrorMessage(errors));
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        
        #region Refresh Token Methods
        
        /// <summary>
        /// Login with refresh token support
        /// </summary>
        public async Task<AuthTokenResponseDto?> LoginWithRefreshAsync(LoginRequestDto loginDto, string? ipAddress)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return null;
            }

            user.LastLoginAt = DateTime.UtcNow;
            
            // Generate tokens
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var accessTokenExpirationMinutes = jwtSettings.GetValue<int>("AccessTokenExpirationMinutes", 30);
            var refreshTokenExpirationDays = jwtSettings.GetValue<int>("RefreshTokenExpirationDays", 7);
            
            var accessToken = GenerateJwtToken(user, accessTokenExpirationMinutes);
            var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress, refreshTokenExpirationDays);
            
            await _context.SaveChangesAsync();

            return new AuthTokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                AccessTokenExpires = DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes),
                RefreshTokenExpires = refreshToken.ExpiresAt,
                User = MapToUserDto(user)
            };
        }
        
        /// <summary>
        /// Refresh token ile yeni access token al
        /// Token rotation: Her kullanımda yeni refresh token verilir
        /// </summary>
        public async Task<AuthTokenResponseDto?> RefreshTokenAsync(string refreshToken, string? ipAddress)
        {
            var token = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);
            
            if (token == null)
            {
                return null;
            }
            
            // Token aktif değilse reject
            if (!token.IsActive)
            {
                // Güvenlik: Kullanılmış/revoke edilmiş token ile gelindiyse tüm tokenları iptal et
                if (token.IsUsed || token.IsRevoked)
                {
                    await RevokeAllUserTokensAsync(token.UserId, ipAddress);
                }
                return null;
            }
            
            // User aktif değilse reject
            if (!token.User.IsActive)
            {
                return null;
            }
            
            // Token rotation: Eski tokeni işaretle, yenisini oluştur
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var accessTokenExpirationMinutes = jwtSettings.GetValue<int>("AccessTokenExpirationMinutes", 30);
            var refreshTokenExpirationDays = jwtSettings.GetValue<int>("RefreshTokenExpirationDays", 7);
            
            var newRefreshToken = await CreateRefreshTokenAsync(token.UserId, ipAddress, refreshTokenExpirationDays);
            
            token.IsUsed = true;
            token.ReplacedByTokenId = newRefreshToken.Id;
            
            var accessToken = GenerateJwtToken(token.User, accessTokenExpirationMinutes);
            
            await _context.SaveChangesAsync();

            return new AuthTokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token,
                AccessTokenExpires = DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes),
                RefreshTokenExpires = newRefreshToken.ExpiresAt,
                User = MapToUserDto(token.User)
            };
        }
        
        /// <summary>
        /// Tek bir refresh tokeni iptal et (logout)
        /// </summary>
        public async Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == refreshToken);
            
            if (token == null || !token.IsActive)
            {
                return false;
            }
            
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            
            await _context.SaveChangesAsync();
            return true;
        }
        
        /// <summary>
        /// Kullanıcının tüm refresh tokenlerini iptal et (güvenlik breach, şifre değişikliği vb.)
        /// </summary>
        public async Task RevokeAllUserTokensAsync(int userId, string? ipAddress)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && !r.IsRevoked && !r.IsUsed)
                .ToListAsync();
            
            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = ipAddress;
            }
            
            await _context.SaveChangesAsync();
        }
        
        #endregion
        
        #region Private Helper Methods
        
        private string GenerateJwtToken(User user, int expirationMinutes)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        
        private async Task<RefreshToken> CreateRefreshTokenAsync(int userId, string? ipAddress, int expirationDays)
        {
            var refreshToken = new RefreshToken
            {
                Token = GenerateSecureRandomToken(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expirationDays),
                CreatedByIp = ipAddress
            };
            
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            
            return refreshToken;
        }
        
        private static string GenerateSecureRandomToken()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
        
        private static UserDto MapToUserDto(User user) => new()
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
        };
        
        #endregion
    }
}







