using TeknikServisAPI.Constants;

namespace TeknikServisAPI.DTOs
{
    public class ServiceFormDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = ServiceFormStatuses.Draft;
        
        // Alan 1
        public DateTime FormDate { get; set; }
        public string? InstitutionName { get; set; }
        public bool InstitutionIsOther { get; set; }
        public string? InstitutionOtherName { get; set; }
        public string? Address { get; set; }
        public string? ContactFirstName { get; set; }
        public string? ContactLastName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime? ServiceStartDate { get; set; }
        public DateTime? ServiceEndDate { get; set; }
        public string? ServiceScope { get; set; }
        public string? ServiceScopeOther { get; set; }
        public string? ServiceType { get; set; }
        public string? ServiceTypeOther { get; set; }
        
        // Alan 2
        public string? ReportedIssue { get; set; }
        public string? WorkPerformed { get; set; }
        public string? MaterialsUsed { get; set; }
        public string? IssueStatus { get; set; }
        public string? IssueStatusOther { get; set; }
        public string? PartsToReplace { get; set; }
        
        // Alan 3
        public string? Photos { get; set; }
        
        // Alan 4
        public string? EtisanFirstName { get; set; }
        public string? EtisanLastName { get; set; }
        public string? EtisanSignature { get; set; }
        public string? AuthorityFirstName { get; set; }
        public string? AuthorityLastName { get; set; }
        public string? AuthorityEmail { get; set; }
        public string? AuthorityPhone { get; set; }
        public string? AuthoritySignature { get; set; }
        
        // Metadata
        public int? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }
    
    public class CreateServiceFormDto
    {
        // Alan 1
        public DateTime? FormDate { get; set; }
        public string? InstitutionName { get; set; }
        public bool InstitutionIsOther { get; set; }
        public string? InstitutionOtherName { get; set; }
        public string? Address { get; set; }
        public string? ContactFirstName { get; set; }
        public string? ContactLastName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime? ServiceStartDate { get; set; }
        public DateTime? ServiceEndDate { get; set; }
        public string? ServiceScope { get; set; }
        public string? ServiceScopeOther { get; set; }
        public string? ServiceType { get; set; }
        public string? ServiceTypeOther { get; set; }
        
        // Alan 2
        public string? ReportedIssue { get; set; }
        public string? WorkPerformed { get; set; }
        public string? MaterialsUsed { get; set; }
        public string? IssueStatus { get; set; }
        public string? IssueStatusOther { get; set; }
        public string? PartsToReplace { get; set; }
        
        // Alan 3
        public string? Photos { get; set; }
        
        // Alan 4
        public string? EtisanFirstName { get; set; }
        public string? EtisanLastName { get; set; }
        public string? EtisanSignature { get; set; }
        public string? AuthorityFirstName { get; set; }
        public string? AuthorityLastName { get; set; }
        public string? AuthorityEmail { get; set; }
        public string? AuthorityPhone { get; set; }
        public string? AuthoritySignature { get; set; }
    }
    
    public class UpdateServiceFormDto
    {
        // Alan 1
        public DateTime? FormDate { get; set; }
        public string? InstitutionName { get; set; }
        public bool? InstitutionIsOther { get; set; }
        public string? InstitutionOtherName { get; set; }
        public string? Address { get; set; }
        public string? ContactFirstName { get; set; }
        public string? ContactLastName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime? ServiceStartDate { get; set; }
        public DateTime? ServiceEndDate { get; set; }
        public string? ServiceScope { get; set; }
        public string? ServiceScopeOther { get; set; }
        public string? ServiceType { get; set; }
        public string? ServiceTypeOther { get; set; }
        
        // Alan 2
        public string? ReportedIssue { get; set; }
        public string? WorkPerformed { get; set; }
        public string? MaterialsUsed { get; set; }
        public string? IssueStatus { get; set; }
        public string? IssueStatusOther { get; set; }
        public string? PartsToReplace { get; set; }
        
        // Alan 3
        public string? Photos { get; set; }
        
        // Alan 4
        public string? EtisanFirstName { get; set; }
        public string? EtisanLastName { get; set; }
        public string? EtisanSignature { get; set; }
        public string? AuthorityFirstName { get; set; }
        public string? AuthorityLastName { get; set; }
        public string? AuthorityEmail { get; set; }
        public string? AuthorityPhone { get; set; }
        public string? AuthoritySignature { get; set; }
    }
    
    public class ServiceFormListDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = ServiceFormStatuses.Draft;
        public DateTime FormDate { get; set; }
        public string? InstitutionName { get; set; }
        public string? InstitutionOtherName { get; set; }
        public string? IssueStatus { get; set; }
        public string? PartsToReplace { get; set; }
        public DateTime? ServiceStartDate { get; set; }
        public DateTime? ServiceEndDate { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }
}
