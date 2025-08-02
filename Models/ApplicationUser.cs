using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AgroProductEcommerce.Models
{
    public enum UserType
    {
        Buyer,
        Supplier,
        Admin,
        Both
    }

    public enum KYCStatus
    {
        Pending,
        Approved,
        Verified,
        Rejected,
        UnderReview,
        NotSubmitted
    }

    public enum BusinessType
    {
        Individual,
        SmallBusiness,
        Corporation,
        Manufacturer,
        Distributor,
        Wholesaler
    }

    public class ApplicationUser : IdentityUser
    {
        // Basic Information
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? CompanyName { get; set; }
        public string? BusinessRegistrationNumber { get; set; }
        public BusinessType BusinessType { get; set; }
        public UserType UserType { get; set; }
        
        // Address Information
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        
        // Business Information
        public string? TaxIdentificationNumber { get; set; }
        public string? Website { get; set; }
        public string? BusinessDescription { get; set; }
        public int? YearsInBusiness { get; set; }
        public string? Industry { get; set; }
        
        // KYC and Verification
        public KYCStatus KYCStatus { get; set; } = KYCStatus.Pending;
        public DateTime? KYCVerifiedDate { get; set; }
        public string? KYCVerifiedBy { get; set; }
        public string? KYCRejectionReason { get; set; }
        
        // Financial Information
        public string? PreferredCurrency { get; set; } = "USD";
        public string? BankAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? SwiftCode { get; set; }
        
        // Platform Settings
        public bool EmailNotifications { get; set; } = true;
        public bool SMSNotifications { get; set; } = false;
        public string? PreferredLanguage { get; set; } = "en";
        public string? TimeZone { get; set; }
        
        // Analytics and Metrics
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginDate { get; set; }
        public int TotalOrders { get; set; } = 0;
        public decimal TotalSpent { get; set; } = 0;
        public int TotalProducts { get; set; } = 0; // For suppliers
        
        // Trade Assurance
        public bool TradeAssuranceEnabled { get; set; } = true;
        public decimal TradeAssuranceLimit { get; set; } = 10000;
        public int TradeAssuranceScore { get; set; } = 0;
        
        // GDPR/CCPA Compliance
        public bool DataProcessingConsent { get; set; } = false;
        public DateTime? DataProcessingConsentDate { get; set; }
        public bool MarketingConsent { get; set; } = false;
        public DateTime? MarketingConsentDate { get; set; }
        
        // Navigation Properties
        public virtual ICollection<SupplierProfile> SupplierProfiles { get; set; }
        public virtual ICollection<BuyerProfile> BuyerProfiles { get; set; }
        public virtual ICollection<Message> SentMessages { get; set; }
        public virtual ICollection<Message> ReceivedMessages { get; set; }
        public virtual ICollection<RFQ> CreatedRFQs { get; set; }
        public virtual ICollection<RFQResponse> RFQResponses { get; set; }
        public virtual ICollection<Dispute> Disputes { get; set; }
        public virtual ICollection<Dispute> ResolvedDisputes { get; set; }
    }
}