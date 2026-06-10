using FluentValidation;
using TeknikServisAPI.DTOs;

namespace TeknikServisAPI.Validators
{
    /// <summary>
    /// UpdateRequestDto için validation kuralları
    /// </summary>
    public class UpdateRequestDtoValidator : AbstractValidator<UpdateRequestDto>
    {
        public UpdateRequestDtoValidator()
        {
            RuleFor(x => x.DeviceSerialNumber)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.DeviceSerialNumber))
                .WithMessage("Seri numarası en fazla 100 karakter olabilir");

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrEmpty(x.Description))
                .WithMessage("Açıklama en fazla 2000 karakter olabilir");

            RuleFor(x => x.InstitutionName)
                .MaximumLength(200)
                .When(x => !string.IsNullOrEmpty(x.InstitutionName))
                .WithMessage("Kurum adı en fazla 200 karakter olabilir");

            RuleFor(x => x.ContactPhone)
                .Must(phone => string.IsNullOrWhiteSpace(phone) || 
                               System.Text.RegularExpressions.Regex.IsMatch(phone.Trim(), @"^[0-9\s\+\-\(\)]{10,20}$"))
                .When(x => x.ContactPhone != null)
                .WithMessage("Geçerli bir telefon numarası giriniz");

            RuleFor(x => x.ContactEmail)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.ContactEmail))
                .WithMessage("Geçerli bir email adresi giriniz");

            RuleFor(x => x.ContactPerson)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.ContactPerson))
                .WithMessage("İletişim kişisi en fazla 100 karakter olabilir");

            RuleFor(x => x.ShippingCity)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.ShippingCity));

            RuleFor(x => x.ShippingDistrict)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.ShippingDistrict));

            RuleFor(x => x.ShippingAddressLine)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.ShippingAddressLine))
                .WithMessage("Adres en fazla 500 karakter olabilir");

            // Stok kalemi validation
            RuleForEach(x => x.StockItems).ChildRules(item =>
            {
                item.RuleFor(i => i.StockItemId)
                    .GreaterThan(0)
                    .WithMessage("Geçerli bir stok kalemi seçiniz");
                
                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Miktar 0'dan büyük olmalı");
            }).When(x => x.StockItems != null);
        }
    }
}
