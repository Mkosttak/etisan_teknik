using FluentAssertions;
using TeknikServisAPI.Validators;

namespace TeknikServisAPI.Tests.Validators
{
    /// <summary>
    /// Tests for PasswordValidator to ensure production-grade password security
    /// Coverage Target: 100% (kritik güvenlik component)
    /// 
    /// Test Senaryoları:
    /// - Valid passwords: Tüm gereksinimleri karşılayan
    /// - Length tests: Min 12 karakter kontrolü
    /// - Complexity tests: Büyük/küçük harf, rakam, özel karakter
    /// - Common passwords: Yaygın şifrelerin reddedilmesi
    /// - Sequential chars: abc, 123 gibi ardışık karakterler
    /// - Repeating chars: aaa, 111 gibi tekrarlayan karakterler
    /// - Edge cases: null, empty, whitespace
    /// </summary>
    public class PasswordValidatorTests
    {
        #region Valid Password Tests
        
        [Theory]
        [InlineData("ValidP@ssw0rd")]
        [InlineData("SuperSecure!2o24")]  // No "123" sequence
        [InlineData("MyP@ssw0rd15")]      // 1 and 5, not sequential
        [InlineData("Tr0ng!P@ssword")]
        [InlineData("C0mpl3x&S3cur3")]
        public void Validate_ValidPassword_ReturnsTrue(string password)
        {
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            isValid.Should().BeTrue(because: "password meets all requirements");
            errors.Should().BeEmpty(because: "no validation errors should exist");
        }
        
        #endregion
        
        #region Length Tests
        
        [Theory]
        [InlineData("Short1!")]      // 7 chars
        [InlineData("Eleven11!")]    // 9 chars  
        [InlineData("TenChrs1!")]    // 10 chars
        [InlineData("Exact11Chr!")]  // 11 chars (1 short)
        public void Validate_TooShort_ReturnsFalse(string password)
        {
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            isValid.Should().BeFalse(because: "password is shorter than 12 characters");
            errors.Should().Contain(e => e.Contains("12 karakter"),
                because: "error should mention minimum length requirement");
        }
        
        [Fact]
        public void Validate_ExactlyTwelveChars_AndValid_ReturnsTrue()
        {
            // Arrange - Exactly 12 characters with all requirements, no sequential
            var password = "V@lidP@ss1!w";  // 12 chars, no sequential
            
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            isValid.Should().BeTrue(because: "12 characters is the minimum valid length");
            errors.Should().BeEmpty();
        }
        
        #endregion
        
        #region Complexity Tests
        
        [Fact]
        public void Validate_NoUpperCase_ReturnsFalse()
        {
            // Arrange
            var password = "nouppercase123!";
            
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            isValid.Should().BeFalse();
            errors.Should().Contain(e => e.Contains("büyük harf"),
                because: "password lacks uppercase letter");
        }
        
        [Fact]
        public void Validate_NoLowerCase_ReturnsFalse()
        {
            // Arrange
            var password = "NOLOWERCASE123!";
            
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            isValid.Should().BeFalse();
            errors.Should().Contain(e => e.Contains("küçük harf"),
                because: "password lacks lowercase letter");
        }
        
        [Fact]
        public void Validate_NoDigit_ReturnsFalse()
        {
            // Arrange
            var password = "NoDigitsHere!AA";
            
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            isValid.Should().BeFalse();
            errors.Should().Contain(e => e.Contains("rakam"),
                because: "password lacks digit");
        }
        
        [Fact]
        public void Validate_NoSpecialChar_ReturnsFalse()
        {
            // Arrange
            var password = "NoSpecialChar123";
            
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            isValid.Should().BeFalse();
            errors.Should().Contain(e => e.Contains("özel karakter"),
                because: "password lacks special character");
        }
        
        [Theory]
        [InlineData("!")]
        [InlineData("@")]
        [InlineData("#")]
        [InlineData("$")]
        [InlineData("%")]
        [InlineData("^")]
        [InlineData("&")]
        [InlineData("*")]
        [InlineData("(")]
        [InlineData(")")]
        public void Validate_WithDifferentSpecialChars_ReturnsTrue(string specialChar)
        {
            // Arrange - All requirements with different special chars, no sequential
            var password = $"V@lidP@ss1{specialChar}w";  // No sequential chars
            
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            isValid.Should().BeTrue(because: $"'{specialChar}' is a valid special character");
        }
        
        #endregion
        
        #region Common Password Tests
        
        [Theory]
        [InlineData("password123!")]
        [InlineData("Password123!")]  // Case insensitive check
        [InlineData("PASSWORD123!")]
        [InlineData("admin123!")]
        [InlineData("Admin123!")]
        [InlineData("ADMIN123!")]
        [InlineData("qwerty123!")]
        [InlineData("Qwerty123!")]
        public void Validate_CommonPassword_ReturnsFalse(string password)
        {
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            isValid.Should().BeFalse(because: "common passwords should be rejected");
            errors.Should().Contain(e => e.Contains("yaygın"),
                because: "error should mention common password");
        }
        
        #endregion
        
        #region Sequential Character Tests
        
        [Theory]
        [InlineData("Abcdefgh123!")]  // abc sequence
        [InlineData("Password123!")]  // 123 sequence
        [InlineData("Xyz98765!@#")]   // xyz sequence
        [InlineData("Defghijk12!")]   // def sequence
        public void Validate_SequentialChars_ReturnsFalse(string password)
        {
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            isValid.Should().BeFalse(because: "sequential characters should be rejected");
            errors.Should().Contain(e => e.Contains("ardışık"),
                because: "error should mention sequential characters");
        }
        
        #endregion
        
        #region Repeating Character Tests
        
        [Theory]
        [InlineData("Passssword1!")]  // ssss
        [InlineData("Password111!")]  // 111
        [InlineData("Paaassword1!")]  // aaa
        [InlineData("Password1!!!")]  // !!!
        public void Validate_RepeatingChars_ReturnsFalse(string password)
        {
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            isValid.Should().BeFalse(because: "repeating characters should be rejected");
            errors.Should().Contain(e => e.Contains("tekrarlayan"),
                because: "error should mention repeating characters");
        }
        
        #endregion
        
        #region Edge Cases
        
        [Fact]
        public void Validate_EmptyString_ReturnsFalse()
        {
            // Act
            var (isValid, errors) = PasswordValidator.Validate("");
            
            // Assert
            isValid.Should().BeFalse();
            errors.Should().NotBeEmpty(because: "empty password should have validation errors");
        }
        
        [Fact]
        public void Validate_NullString_ReturnsFalse()
        {
            // Act
            var (isValid, errors) = PasswordValidator.Validate(null!);
            
            // Assert
            isValid.Should().BeFalse();
            errors.Should().NotBeEmpty(because: "null password should have validation errors");
        }
        
        [Fact]
        public void Validate_WhitespaceOnly_ReturnsFalse()
        {
            // Arrange
            var password = "            ";  // 12 spaces
            
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            isValid.Should().BeFalse();
            errors.Should().NotBeEmpty(because: "whitespace-only password should be invalid");
        }
        
        [Fact]
        public void Validate_WithSpaces_ButOtherwiseValid_ReturnsTrue()
        {
            // Arrange - Spaces are allowed if other requirements met, no sequential
            var password = "V@lid P@ss1!";  // No sequential chars
            
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            // This test documents current behavior - adjust if spaces should be disallowed
            isValid.Should().BeTrue(because: "spaces are currently allowed in passwords");
        }
        
        #endregion
        
        #region Error Message Tests
        
        [Fact]
        public void Validate_InvalidPassword_ReturnsMultipleErrors()
        {
            // Arrange - Missing multiple requirements
            var password = "short";
            
            // Act
            var (isValid, errors) = PasswordValidator.Validate(password);
            
            // Assert
            isValid.Should().BeFalse();
            errors.Should().HaveCountGreaterThan(1,
                because: "multiple validation rules are violated");
            errors.Should().Contain(e => e.Contains("12 karakter"));
            errors.Should().Contain(e => e.Contains("büyük harf"));
            errors.Should().Contain(e => e.Contains("rakam"));
            errors.Should().Contain(e => e.Contains("özel karakter"));
        }
        
        [Fact]
        public void GetUserFriendlyErrorMessage_WithErrors_ReturnsFormattedMessage()
        {
            // Arrange
            var errors = new List<string>
            {
                "Şifre en az 12 karakter olmalı",
                "En az 1 büyük harf içermeli"
            };
            
            // Act
            var message = PasswordValidator.GetUserFriendlyErrorMessage(errors);
            
            // Assert
            message.Should().NotBeNullOrEmpty();
            message.Should().Contain("Şifre gereksinimleri");
            message.Should().Contain("12 karakter");
            message.Should().Contain("büyük harf");
        }
        
        [Fact]
        public void GetUserFriendlyErrorMessage_WithNoErrors_ReturnsEmpty()
        {
            // Arrange
            var errors = new List<string>();
            
            // Act
            var message = PasswordValidator.GetUserFriendlyErrorMessage(errors);
            
            // Assert
            message.Should().BeEmpty();
        }
        
        #endregion
    }
}
