using FluentValidation;
using TeknikServisAPI.DTOs;

namespace TeknikServisAPI.Validators
{
    /// <summary>
    /// CreateServiceFormDto için validation kuralları
    /// </summary>
    public class CreateServiceFormDtoValidator : AbstractValidator<CreateServiceFormDto>
    {
        public CreateServiceFormDtoValidator()
        {
            // Alan 1 - Kurum ve İletişim
            RuleFor(x => x.InstitutionName)
                .MaximumLength(200)
                .When(x => !string.IsNullOrEmpty(x.InstitutionName))
                .WithMessage("Kurum adı en fazla 200 karakter olabilir");

            RuleFor(x => x.InstitutionOtherName)
                .MaximumLength(200)
                .When(x => !string.IsNullOrEmpty(x.InstitutionOtherName))
                .WithMessage("Diğer kurum adı en fazla 200 karakter olabilir");

            RuleFor(x => x.Address)
                .MaximumLength(500)
                .When(x => !string.IsNullOrEmpty(x.Address))
                .WithMessage("Adres en fazla 500 karakter olabilir");

            RuleFor(x => x.ContactFirstName)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.ContactFirstName));

            RuleFor(x => x.ContactLastName)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.ContactLastName));

            RuleFor(x => x.Phone)
                .Matches(@"^[0-9\s\+\-\(\)]{10,20}$")
                .When(x => !string.IsNullOrEmpty(x.Phone))
                .WithMessage("Geçerli bir telefon numarası giriniz");

            RuleFor(x => x.Email)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.Email))
                .WithMessage("Geçerli bir email adresi giriniz");

            // Tarih validasyonu - bitiş tarihi başlangıçtan sonra olmalı
            RuleFor(x => x.ServiceEndDate)
                .GreaterThanOrEqualTo(x => x.ServiceStartDate)
                .When(x => x.ServiceStartDate.HasValue && x.ServiceEndDate.HasValue)
                .WithMessage("Servis bitiş tarihi, başlangıç tarihinden sonra olmalı");

            // Alan 2 - Çalışma Detayları
            RuleFor(x => x.ReportedIssue)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrEmpty(x.ReportedIssue))
                .WithMessage("Bildirilen sorun en fazla 2000 karakter olabilir");

            RuleFor(x => x.WorkPerformed)
                .MaximumLength(2000)
                .When(x => !string.IsNullOrEmpty(x.WorkPerformed))
                .WithMessage("Yapılan iş en fazla 2000 karakter olabilir");

            RuleFor(x => x.MaterialsUsed)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.MaterialsUsed))
                .WithMessage("Kullanılan malzemeler en fazla 1000 karakter olabilir");

            RuleFor(x => x.PartsToReplace)
                .MaximumLength(1000)
                .When(x => !string.IsNullOrEmpty(x.PartsToReplace))
                .WithMessage("Değiştirilecek parçalar en fazla 1000 karakter olabilir");

            // Alan 4 - İmza Bilgileri
            RuleFor(x => x.EtisanFirstName)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.EtisanFirstName));

            RuleFor(x => x.EtisanLastName)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.EtisanLastName));

            RuleFor(x => x.AuthorityFirstName)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.AuthorityFirstName));

            RuleFor(x => x.AuthorityLastName)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.AuthorityLastName));

            RuleFor(x => x.AuthorityEmail)
                .EmailAddress()
                .When(x => !string.IsNullOrEmpty(x.AuthorityEmail))
                .WithMessage("Yetkili email adresi geçerli olmalı");

            RuleFor(x => x.AuthorityPhone)
                .Matches(@"^[0-9\s\+\-\(\)]{10,20}$")
                .When(x => !string.IsNullOrEmpty(x.AuthorityPhone))
                .WithMessage("Yetkili telefon numarası geçerli olmalı");
        }
    }
}
