using FluentValidation;
using TeknikServisAPI.DTOs;

namespace TeknikServisAPI.Validators
{
    public class CreateStockItemDtoValidator : AbstractValidator<CreateStockItemDto>
    {
        public CreateStockItemDtoValidator()
        {
            RuleFor(x => x.ItemName)
                .NotEmpty()
                .WithMessage("Ürün adı zorunludur")
                .MaximumLength(200)
                .WithMessage("Ürün adı en fazla 200 karakter olabilir");

            RuleFor(x => x.CurrentStock)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Stok miktarı negatif olamaz");

            RuleFor(x => x.MinimumStock)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minimum stok negatif olamaz");
        }
    }
}





