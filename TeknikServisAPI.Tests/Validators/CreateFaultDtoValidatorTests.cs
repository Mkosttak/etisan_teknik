using FluentAssertions;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Validators;

namespace TeknikServisAPI.Tests.Validators
{
    /// <summary>
    /// CreateFaultDtoValidator testleri
    /// FluentValidation kurallarının doğru çalıştığını doğrular
    /// </summary>
    public class CreateFaultDtoValidatorTests
    {
        private readonly CreateFaultDtoValidator _validator;

        public CreateFaultDtoValidatorTests()
        {
            _validator = new CreateFaultDtoValidator();
        }

        #region Valid DTO Tests

        [Fact]
        public void Validate_ValidDto_ReturnsNoErrors()
        {
            // Arrange - CreateFaultDto'da hiçbir alan zorunlu değil
            var dto = new CreateFaultDto
            {
                DeviceSerialNumber = "ABC123456",
                Description = "Cihaz açılmıyor",
                InstitutionName = "Test Kurumu",
                ContactPerson = "Test User",
                ContactPhone = "+90 555 123 4567",
                ContactEmail = "test@example.com"
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_EmptyDto_ReturnsNoErrors()
        {
            // Arrange - Tüm alanlar opsiyonel olduğu için boş DTO da valid
            var dto = new CreateFaultDto();

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        #endregion

        #region Max Length Tests

        [Fact]
        public void Validate_DeviceSerialNumberTooLong_ReturnsError()
        {
            // Arrange
            var dto = new CreateFaultDto
            {
                DeviceSerialNumber = new string('A', 101) // max 100
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "DeviceSerialNumber");
        }

        [Fact]
        public void Validate_DescriptionTooLong_ReturnsError()
        {
            // Arrange
            var dto = new CreateFaultDto
            {
                Description = new string('a', 2001) // max 2000
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Description");
        }

        #endregion

        #region Email Validation Tests

        [Theory]
        [InlineData("valid@email.com")]
        [InlineData("test.user@domain.org")]
        [InlineData("user+tag@company.co.tr")]
        public void Validate_ValidEmail_ReturnsNoEmailError(string email)
        {
            // Arrange
            var dto = new CreateFaultDto
            {
                ContactEmail = email
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.Errors.Should().NotContain(e => e.PropertyName == "ContactEmail");
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("@nodomain.com")]
        [InlineData("no-at-sign")]
        public void Validate_InvalidEmail_ReturnsError(string email)
        {
            // Arrange
            var dto = new CreateFaultDto
            {
                ContactEmail = email
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "ContactEmail");
        }

        #endregion

        #region Phone Validation Tests

        [Theory]
        [InlineData("+90 555 123 4567")]
        [InlineData("05551234567")]
        [InlineData("(0555) 123-4567")]
        public void Validate_ValidPhone_ReturnsNoPhoneError(string phone)
        {
            // Arrange
            var dto = new CreateFaultDto
            {
                ContactPhone = phone
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.Errors.Should().NotContain(e => e.PropertyName == "ContactPhone");
        }

        [Theory]
        [InlineData("123")] // Too short
        [InlineData("abcdefghij")] // Not numeric
        public void Validate_InvalidPhone_ReturnsError(string phone)
        {
            // Arrange
            var dto = new CreateFaultDto
            {
                ContactPhone = phone
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "ContactPhone");
        }

        #endregion
    }
}
