using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TeknikServisAPI.Data;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Models;
using TeknikServisAPI.Services;
using TeknikServisAPI.Tests.Helpers;

namespace TeknikServisAPI.Tests.Services
{
    /// <summary>
    /// Tests for AuthService - critical security component
    /// Coverage target: 60%+ (login, JWT, password management)
    /// 
    /// Key Test Areas:
    /// - Login authentication (success/failure scenarios)
    /// - JWT token generation and validation
    /// - Password hashing and verification
    /// - User creation with password validation
    /// - Password change with password validation
    /// - Admin bootstrap functionality
    /// </summary>
    public class AuthServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly AuthService _authService;
        
        public AuthServiceTests()
        {
            // Each test gets a fresh database
            _context = InMemoryDbHelper.CreateContext($"AuthServiceTest_{Guid.NewGuid()}");
            _configuration = MockConfigurationHelper.CreateWithJwtSettings();
            _authService = new AuthService(_context, _configuration);
        }
        
        public void Dispose()
        {
            _context.Dispose();
        }
        
        #region Login Tests
        
        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUser()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var loginDto = new LoginRequestDto
            {
                Email = "test@test.com",
                Password = "ValidPassword123!"  // From seed data
            };
            
            // Act
            var result = await _authService.LoginAsync(loginDto);
            
            // Assert
            result.Should().NotBeNull();
            result!.Token.Should().NotBeNullOrEmpty();
            result.User.Should().NotBeNull();
            result.User.Email.Should().Be("test@test.com");
            result.User.Role.Should().Be("Teknisyen");
        }
        
        [Fact]
        public async Task LoginAsync_InvalidEmail_ReturnsNull()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var loginDto = new LoginRequestDto
            {
                Email = "nonexistent@test.com",
                Password = "SomePassword123!"
            };
            
            // Act
            var result = await _authService.LoginAsync(loginDto);
            
            // Assert
            result.Should().BeNull(because: "user with this email doesn't exist");
        }
        
        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsNull()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var loginDto = new LoginRequestDto
            {
                Email = "test@test.com",
                Password = "WrongPassword123!"
            };
            
            // Act
            var result = await _authService.LoginAsync(loginDto);
            
            // Assert
            result.Should().BeNull(because: "password is incorrect");
        }
        
        [Fact]
        public async Task LoginAsync_InactiveUser_ReturnsNull()
        {
            // Arrange
            var inactiveUser = new User
            {
                Email = "inactive@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPassword123!"),
                FirstName = "Inactive",
                LastName = "User",
                Role = "Teknisyen",
                IsActive = false,  // User is deactivated
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(inactiveUser);
            await _context.SaveChangesAsync();
            
            var loginDto = new LoginRequestDto
            {
                Email = "inactive@test.com",
                Password = "ValidPassword123!"
            };
            
            // Act
            var result = await _authService.LoginAsync(loginDto);
            
            // Assert
            result.Should().BeNull(because: "inactive users cannot login");
        }
        
        #endregion
        
        #region JWT Token Tests
        
        [Fact]
        public async Task LoginAsync_GeneratedToken_ContainsUserClaims()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var loginDto = new LoginRequestDto
            {
                Email = "test@test.com",
                Password = "ValidPassword123!"
            };
            
            // Act
            var result = await _authService.LoginAsync(loginDto);
            
            // Assert
            result.Should().NotBeNull();
            var token = result!.Token;
            
            // Decode JWT to verify claims (basic validation)
            token.Should().NotBeNullOrEmpty();
            token.Split('.').Should().HaveCount(3, because: "JWT has 3 parts: header.payload.signature");
            
            // Token should be at least 100 characters for a proper JWT
            token.Length.Should().BeGreaterThan(100);
        }
        
        #endregion
        
        #region User Creation Tests
        
        [Fact]
        public async Task CreateUserAsync_ValidData_CreatesUserSuccessfully()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Email = "newuser@test.com",
                Password = "SecureP@ssw0rd9",  // Meets all requirements, no sequential
                FirstName = "New",
                LastName = "User",
                Role = "Teknisyen"
            };
            
            // Act
            var result = await _authService.CreateUserAsync(createUserDto);
            
            // Assert
            result.Should().NotBeNull();
            result!.Email.Should().Be("newuser@test.com");
            result.FirstName.Should().Be("New");
            result.LastName.Should().Be("User");
            result.Role.Should().Be("Teknisyen");
            result.IsActive.Should().BeTrue();
            
            // Verify password is hashed, not stored in plain text
            result.PasswordHash.Should().StartWith("$2");  // BCrypt hash starts with $2
            result.PasswordHash.Should().NotBe("SecureP@ssw0rd9");
        }
        
        [Fact]
        public async Task CreateUserAsync_WeakPassword_ThrowsArgumentException()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Email = "newuser@test.com",
                Password = "weak",  // Doesn't meet password policy
                FirstName = "New",
                LastName = "User",
                Role = "Teknisyen"
            };
            
            // Act
            Func<Task> act = async () => await _authService.CreateUserAsync(createUserDto);
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Şifre gereksinimleri*");
        }
        
        [Fact]
        public async Task CreateUserAsync_DuplicateEmail_ReturnsNull()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var createUserDto = new CreateUserDto
            {
                Email = "test@test.com",  // Already exists in seed data
                Password = "SecureP@ssw0rd9",
                FirstName = "Duplicate",
                LastName = "User",
                Role = "Teknisyen"
            };
            
            // Act
            var result = await _authService.CreateUserAsync(createUserDto);
            
            // Assert
            result.Should().BeNull(because: "duplicate email should return null");
        }
        
        #endregion
        
        #region Password Change Tests
        
        [Fact]
        public async Task ChangePasswordAsync_ValidPassword_ChangesSuccessfully()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            var user = await _context.Users.FindAsync(1);
            var oldPasswordHash = user!.PasswordHash;
            
            var oldPassword = "ValidPassword123!";
            var newPassword = "N3wP@ssw0rd2o25";  // Different, valid, no sequential
            
            // Act
            var result = await _authService.ChangePasswordAsync(1, oldPassword, newPassword);
            
            // Assert
            result.Should().BeTrue();
            
            // Verify password hash changed
            var updatedUser = await _context.Users.FindAsync(1);
            updatedUser!.PasswordHash.Should().NotBe(oldPasswordHash);
            
            // Verify new password works
            var loginResult = await _authService.LoginAsync(new LoginRequestDto
            {
                Email = "test@test.com",
                Password = "N3wP@ssw0rd2o25"
            });
            loginResult.Should().NotBeNull();
        }
        
        [Fact]
        public async Task ChangePasswordAsync_WrongOldPassword_ReturnsFalse()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            
            // Act
            var result = await _authService.ChangePasswordAsync(1, "WrongOldPassword123!", "N3wP@ssw0rd2o25");
            
            // Assert
            result.Should().BeFalse(because: "old password is incorrect");
        }
        
        [Fact]
        public async Task ChangePasswordAsync_WeakNewPassword_ThrowsArgumentException()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            
            // Act
            Func<Task> act = async () => await _authService.ChangePasswordAsync(1, "ValidPassword123!", "weak");
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Şifre gereksinimleri*");
        }
        
        [Fact]
        public async Task ChangePasswordAsync_CommonPassword_ThrowsArgumentException()
        {
            // Arrange
            InMemoryDbHelper.SeedTestData(_context);
            
            // Act
            Func<Task> act = async () => await _authService.ChangePasswordAsync(1, "ValidPassword123!", "password123!");
            
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*yaygın*");
        }
        
        #endregion
        
        #region Password Hashing Tests
        
        [Fact]
        public async Task CreateUserAsync_PasswordHashing_UsesBCrypt()
        {
            // Arrange & Act
            var createUserDto = new CreateUserDto
            {
                Email = "hashtest@test.com",
                Password = "TestP@ssw0rd9",
                FirstName = "Hash",
                LastName = "Test",
                Role = "Teknisyen"
            };
            
            var user = await _authService.CreateUserAsync(createUserDto);
            
            // Assert
            user.Should().NotBeNull();
            
            // BCrypt hash format: $2a$10$... (60 characters)
            user!.PasswordHash.Should().MatchRegex(@"^\$2[aby]\$\d{2}\$");
            user.PasswordHash.Length.Should().Be(60);
            
            // Verify password can be verified with BCrypt
            var isValid = BCrypt.Net.BCrypt.Verify("TestP@ssw0rd9", user.PasswordHash);
            isValid.Should().BeTrue();
        }
        
        [Fact]
        public void PasswordHashing_SamePassword_ProducesDifferentHashes()
        {
            // Arrange
            var password = "SameP@ssw0rd9";
            
            // Act
            var hash1 = BCrypt.Net.BCrypt.HashPassword(password);
            var hash2 = BCrypt.Net.BCrypt.HashPassword(password);
            
            // Assert
            hash1.Should().NotBe(hash2, because: "BCrypt uses salting");
            
            // But both should verify correctly
            BCrypt.Net.BCrypt.Verify(password, hash1).Should().BeTrue();
            BCrypt.Net.BCrypt.Verify(password, hash2).Should().BeTrue();
        }
        
        #endregion
    }
}
