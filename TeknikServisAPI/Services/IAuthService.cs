/**
 * ═══════════════════════════════════════════════════════════════════════════
 * DOSYA: IAuthService.cs
 * KONUM: TeknikServisAPI/Services/
 * TİP: Backend - Service Interface
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * AMAÇ: Authentication ve User Management service interface
 * 
 * SORUMLULUKLAR:
 * - Login işlemleri (v1: legacy, v2: refresh token)
 * - JWT token generation
 * - Refresh token management (create, refresh, revoke)
 * - User CRUD operations
 * - Password management
 * 
 * IMPLEMENTATION: AuthService.cs
 * KULLANIM: AuthController.cs, UsersController.cs
 * 
 * ═══════════════════════════════════════════════════════════════════════════
 */

using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;

namespace TeknikServisAPI.Services
{
    public interface IAuthService
    {
        /**
         * ═══════════════════════════════════════════════════════════════════
         * LOGIN İŞLEMLERİ
         * ═══════════════════════════════════════════════════════════════════
         */
        
        /// <summary>
        /// Legacy login - Sadece access token döner (geriye uyumluluk için)
        /// </summary>
        /// <param name="loginDto">Email ve password</param>
        /// <returns>LoginResponseDto with access token ONLY</returns>
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto);
        
        /// <summary>
        /// Modern login - Access + Refresh token döner (Production için önerilen)
        /// </summary>
        /// <param name="loginDto">Email ve password</param>
        /// <param name="ipAddress">İstemci IP adresi (revoke için)</param>
        /// <returns>AuthTokenResponseDto with access AND refresh tokens</returns>
        Task<AuthTokenResponseDto?> LoginWithRefreshAsync(LoginRequestDto loginDto, string? ipAddress);
        
        /**
         * ═══════════════════════════════════════════════════════════════════
         * JWT TOKEN İŞLEMLERİ
         * ═══════════════════════════════════════════════════════════════════
         */
        
        /// <summary>
        /// JWT access token oluştur (15 dakika lifetime)
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>JWT string</returns>
        string GenerateJwtToken(User user);
        
        /**
         * ═══════════════════════════════════════════════════════════════════
         * REFRESH TOKEN İŞLEMLERİ
         * ═══════════════════════════════════════════════════════════════════
         */
        
        /// <summary>
        /// Refresh token ile yeni access token al
        /// </summary>
        /// <param name="refreshToken">Mevcut refresh token</param>
        /// <param name="ipAddress">İstemci IP (güvenlik için)</param>
        /// <returns>Yeni access + refresh token</returns>
        Task<AuthTokenResponseDto?> RefreshTokenAsync(string refreshToken, string? ipAddress);
        
        /// <summary>
        /// Tek refresh token'ı iptal et (logout)
        /// </summary>
        Task<bool> RevokeTokenAsync(string refreshToken, string? ipAddress);
        
        /// <summary>
        /// Kullanıcının tüm token'larını iptal et (güvenlik breach)
        /// </summary>
        Task RevokeAllUserTokensAsync(int userId, string? ipAddress);
        
        /**
         * ═══════════════════════════════════════════════════════════════════
         * USER CRUD İŞLEMLERİ
         * ═══════════════════════════════════════════════════════════════════
         */
        
        /// <summary>ID ile kullanıcı getir</summary>
        Task<User?> GetUserByIdAsync(int userId);
        
        /// <summary>Email ile kullanıcı getir (login için)</summary>
        Task<User?> GetUserByEmailAsync(string email);
        
        /// <summary>Tüm kullanıcıları listele (Admin only)</summary>
        Task<List<User>> GetAllUsersAsync();
        
        /// <summary>Yeni kullanıcı oluştur</summary>
        Task<User?> CreateUserAsync(CreateUserDto createUserDto);
        
        /// <summary>Kullanıcı bilgilerini güncelle</summary>
        Task<User?> UpdateUserAsync(int userId, UpdateUserDto updateUserDto);
        
        /// <summary>Kullanıcıyı sil (soft delete - IsActive=false)</summary>
        Task<bool> DeleteUserAsync(int userId);
        
        /// <summary>Admin kullanıcı oluştur (bootstrap için)</summary>
        Task<User?> CreateAdminUserAsync();
        
        /// <summary>Şifre değiştir (mevcut şifre kontrolü ile)</summary>
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}
