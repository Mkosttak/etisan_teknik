using FluentValidation;
using TeknikServisAPI.DTOs;

namespace TeknikServisAPI.Validators
{
    public class CreateFaultDtoValidator : AbstractValidator<CreateFaultDto>
    {
        public CreateFaultDtoValidator()
        {
            // Hiçbir alan zorunlu değil, sadece format kontrolleri

            RuleFor(x => x.DeviceSerialNumber)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.DeviceSerialNumber))
                .WithMessage("Seri numarası en fazla 100 karakter olabilir");

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrEmpty(x.Description))
                .WithMessage("Açıklama en fazla 2000 karakter olabilir");

            RuleFor(x => x.ContactPhone)
                .Matches(@"^[0-9\s\+\-\(\)]+$")
                .When(x => !string.IsNullOrEmpty(x.ContactPhone))
                .WithMessage("Telefon numarası sadece rakam, boşluk, +, -, (, ) karakterlerini içerebilir");

            RuleFor(x => x.ContactEmail)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.ContactEmail))
                .WithMessage("Geçerli bir email adresi giriniz");

            RuleFor(x => x.ShippingCity)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.ShippingCity));

            RuleFor(x => x.ShippingDistrict)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.ShippingDistrict));
        }
    }
}



