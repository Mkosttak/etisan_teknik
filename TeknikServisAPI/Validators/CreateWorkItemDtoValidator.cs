using FluentValidation;
using TeknikServisAPI.DTOs;

namespace TeknikServisAPI.Validators
{
    /// <summary>
    /// CreateWorkItemDto için validation kuralları
    /// </summary>
    public class CreateWorkItemDtoValidator : AbstractValidator<CreateWorkItemDto>
    {
        private static readonly string[] ValidKinds = { "Request", "PlannedWork" };
        private static readonly string[] ValidPriorities = { "Low", "Normal", "High", "Critical" };
        private static readonly string[] ValidStages = { "Backlog", "Done", "Cancelled" };

        public CreateWorkItemDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Başlık zorunludur")
                .MaximumLength(200)
                .WithMessage("Başlık en fazla 200 karakter olabilir");

            RuleFor(x => x.Description)
                .MaximumLength(4000)
                .When(x => !string.IsNullOrEmpty(x.Description))
                .WithMessage("Açıklama en fazla 4000 karakter olabilir");

            RuleFor(x => x.Kind)
                .Must(kind => ValidKinds.Contains(kind))
                .WithMessage($"Geçerli iş türleri: {string.Join(", ", ValidKinds)}");

            RuleFor(x => x.Priority)
                .Must(priority => ValidPriorities.Contains(priority))
                .WithMessage($"Geçerli öncelikler: {string.Join(", ", ValidPriorities)}");

            RuleFor(x => x.Stage)
                .Must(stage => ValidStages.Contains(stage))
                .WithMessage($"Geçerli aşamalar: {string.Join(", ", ValidStages)}");

            // Kurum
            RuleFor(x => x.InstitutionName)
                .MaximumLength(200)
                .When(x => !string.IsNullOrEmpty(x.InstitutionName));

            RuleFor(x => x.InstitutionOtherName)
                .MaximumLength(200)
                .When(x => !string.IsNullOrEmpty(x.InstitutionOtherName));

            // İletişim
            RuleFor(x => x.ContactPerson)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.ContactPerson));

            RuleFor(x => x.ContactPhone)
                .Matches(@"^[0-9\s\+\-\(\)]{10,20}$")
                .When(x => !string.IsNullOrEmpty(x.ContactPhone))
                .WithMessage("Geçerli bir telefon numarası giriniz");

            RuleFor(x => x.ContactEmail)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.ContactEmail))
                .WithMessage("Geçerli bir email adresi giriniz");

            // Adres
            RuleFor(x => x.ShippingAddressLine)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.ShippingAddressLine));

            RuleFor(x => x.ShippingCity)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.ShippingCity));

            RuleFor(x => x.ShippingDistrict)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.ShippingDistrict));

            RuleFor(x => x.ShippingPostalCode)
                .MaximumLength(10)
                .When(x => !string.IsNullOrEmpty(x.ShippingPostalCode));

            // Tarih validasyonu - planlanan bitiş, başlangıçtan sonra olmalı
            RuleFor(x => x.PlannedEndAt)
                .GreaterThanOrEqualTo(x => x.PlannedStartAt)
                .When(x => x.PlannedStartAt.HasValue && x.PlannedEndAt.HasValue)
                .WithMessage("Planlanan bitiş tarihi, başlangıç tarihinden sonra olmalı");

            // Etiket validasyonu
            RuleForEach(x => x.Tags)
                .MaximumLength(50)
                .WithMessage("Her etiket en fazla 50 karakter olabilir");
        }
    }
}
