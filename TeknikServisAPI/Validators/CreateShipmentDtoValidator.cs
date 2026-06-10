using FluentValidation;
using TeknikServisAPI.DTOs;

namespace TeknikServisAPI.Validators
{
    /// <summary>
    /// CreateShipmentDto için validation kuralları
    /// </summary>
    public class CreateShipmentDtoValidator : AbstractValidator<CreateShipmentDto>
    {
        private static readonly string[] ValidShipmentTypes = { "ToCustomer", "FromCustomer", "Internal" };

        public CreateShipmentDtoValidator()
        {
            RuleFor(x => x.ShipmentType)
                .Must(type => ValidShipmentTypes.Contains(type))
                .WithMessage($"Geçerli sevkiyat türleri: {string.Join(", ", ValidShipmentTypes)}");

            RuleFor(x => x.TrackingNumber)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.TrackingNumber))
                .WithMessage("Takip numarası en fazla 100 karakter olabilir");

            RuleFor(x => x.CargoCompany)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.CargoCompany))
                .WithMessage("Kargo firması en fazla 100 karakter olabilir");

            RuleFor(x => x.Notes)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.Notes))
                .WithMessage("Notlar en fazla 1000 karakter olabilir");

            // Sevkiyat kalemleri validation
            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("En az bir sevkiyat kalemi eklenmelidir");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ItemType)
                    .NotEmpty()
                    .WithMessage("Kalem türü zorunludur")
                    .Must(type => new[] { "Device", "StockItem", "Other" }.Contains(type))
                    .WithMessage("Geçerli kalem türleri: Device, StockItem, Other");

                item.RuleFor(i => i.Description)
                    .NotEmpty()
                    .WithMessage("Açıklama zorunludur")
                    .MaximumLength(500)
                    .WithMessage("Açıklama en fazla 500 karakter olabilir");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Miktar 0'dan büyük olmalı");

                item.RuleFor(i => i.SerialNumber)
                    .MaximumLength(100)
                    .When(i => !string.IsNullOrEmpty(i.SerialNumber));
            });
        }
    }
}
