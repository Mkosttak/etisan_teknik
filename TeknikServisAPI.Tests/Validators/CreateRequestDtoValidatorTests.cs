using FluentAssertions;
using TeknikServisAPI.DTOs;
using TeknikServisAPI.Validators;

namespace TeknikServisAPI.Tests.Validators
{
    /// <summary>
    /// CreateRequestDtoValidator testleri
    /// FluentValidation kurallarının doğru çalıştığını doğrular
    /// </summary>
    public class CreateRequestDtoValidatorTests
    {
        private readonly CreateRequestDtoValidator _validator;

        public CreateRequestDtoValidatorTests()
        {
            _validator = new CreateRequestDtoValidator();
        }

        #region Valid DTO Tests

        [Fact]
        public void Validate_ValidDto_ReturnsNoErrors()
        {
            // Arrange
            var dto = new CreateRequestDto
            {
                DeviceSerialNumber = "SN123456",
                Description = "Yeni laptop talebi",
                InstitutionName = "Test Kurumu",
                ContactPerson = "Test User",
                ContactPhone = "+90 555 123 4567",
                ContactEmail = "test@example.com",
                ShippingAddressLine = "Test Sokak No:1",
                ShippingCity = "İstanbul",
                ShippingDistrict = "Kadıköy",
                IsUrgent = false
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
            var dto = new CreateRequestDto();

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_UrgentRequest_WithValidData_ReturnsNoErrors()
        {
            // Arrange
            var dto = new CreateRequestDto
            {
                Description = "Acil yazıcı tamiri gerekli",
                IsUrgent = true
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        #endregion

        #region Max Length Tests

        [Fact]
        public void Validate_DescriptionTooLong_ReturnsError()
        {
            // Arrange
            var dto = new CreateRequestDto
            {
                Description = new string('x', 2001) // max 2000
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "Description");
        }

        [Fact]
        public void Validate_DeviceSerialNumberTooLong_ReturnsError()
        {
            // Arrange
            var dto = new CreateRequestDto
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
        public void Validate_InstitutionNameTooLong_ReturnsError()
        {
            // Arrange
            var dto = new CreateRequestDto
            {
                InstitutionName = new string('A', 201) // max 200
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "InstitutionName");
        }

        [Fact]
        public void Validate_ShippingAddressLineTooLong_ReturnsError()
        {
            // Arrange
            var dto = new CreateRequestDto
            {
                ShippingAddressLine = new string('A', 501) // max 500
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "ShippingAddressLine");
        }

        #endregion

        #region Email Validation Tests

        [Theory]
        [InlineData("valid@email.com")]
        [InlineData("test.user@domain.org")]
        public void Validate_ValidEmail_ReturnsNoEmailError(string email)
        {
            // Arrange
            var dto = new CreateRequestDto
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
        public void Validate_InvalidEmail_ReturnsError(string email)
        {
            // Arrange
            var dto = new CreateRequestDto
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
        public void Validate_ValidPhone_ReturnsNoPhoneError(string phone)
        {
            // Arrange
            var dto = new CreateRequestDto
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
            var dto = new CreateRequestDto
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

        #region Stock Items Validation Tests

        [Fact]
        public void Validate_ValidStockItems_ReturnsNoErrors()
        {
            // Arrange
            var dto = new CreateRequestDto
            {
                StockItems = new List<CreateRequestStockItemDto>
                {
                    new() { StockItemId = 1, Quantity = 5 },
                    new() { StockItemId = 2, Quantity = 3 }
                }
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_InvalidStockItemId_ReturnsError()
        {
            // Arrange
            var dto = new CreateRequestDto
            {
                StockItems = new List<CreateRequestStockItemDto>
                {
                    new() { StockItemId = 0, Quantity = 5 } // Invalid ID
                }
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName.Contains("StockItemId"));
        }

        [Fact]
        public void Validate_InvalidStockItemQuantity_ReturnsError()
        {
            // Arrange
            var dto = new CreateRequestDto
            {
                StockItems = new List<CreateRequestStockItemDto>
                {
                    new() { StockItemId = 1, Quantity = 0 } // Invalid quantity
                }
            };

            // Act
            var result = _validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName.Contains("Quantity"));
        }

        #endregion
    }
}
