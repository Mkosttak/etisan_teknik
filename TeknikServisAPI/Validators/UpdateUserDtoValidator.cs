using FluentValidation;
using TeknikServisAPI.Constants;
using TeknikServisAPI.DTOs;

namespace TeknikServisAPI.Validators
{
    /// <summary>
    /// UpdateUserDto için validation kuralları
    /// </summary>
    public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
    {

        public UpdateUserDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .MinimumLength(2)
                .When(x => !string.IsNullOrEmpty(x.FirstName))
                .WithMessage("Ad en az 2 karakter olmalı")
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.FirstName))
                .WithMessage("Ad en fazla 100 karakter olabilir");

            RuleFor(x => x.LastName)
                .MinimumLength(2)
                .When(x => !string.IsNullOrEmpty(x.LastName))
                .WithMessage("Soyad en az 2 karakter olmalı")
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.LastName))
                .WithMessage("Soyad en fazla 100 karakter olabilir");

            RuleFor(x => x.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("Geçerli bir email adresi giriniz");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^[0-9\s\+\-\(\)]{10,20}$")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithMessage("Geçerli bir telefon numarası giriniz");

            RuleFor(x => x.Role)
                .Must(role => AppRoles.AllRoles.Contains(role))
                .When(x => !string.IsNullOrEmpty(x.Role))
                .WithMessage($"Geçerli roller: {string.Join(", ", AppRoles.AllRoles)}");

            // Şifre değişikliği için PasswordValidator kullanılıyor
            // Bu sadece format kontrolü - güçlü şifre kontrolü AuthService'de yapılıyor
            RuleFor(x => x.Password)
                .MinimumLength(12)
                .When(x => !string.IsNullOrEmpty(x.Password))
                .WithMessage("Şifre en az 12 karakter olmalı");
        }
    }
}
