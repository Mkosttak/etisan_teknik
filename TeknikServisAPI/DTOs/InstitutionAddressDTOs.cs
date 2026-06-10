namespace TeknikServisAPI.DTOs
{
    public class InstitutionAddressDto
    {
        public int Id { get; set; }
        public int InstitutionId { get; set; }
        public string? Label { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? AddressLine { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateInstitutionAddressDto
    {
        public string? Label { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? AddressLine { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool IsDefault { get; set; } = false;
    }

    public class UpdateInstitutionAddressDto
    {
        public string? Label { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? AddressLine { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool? IsDefault { get; set; }
    }
}
