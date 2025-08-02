using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroProductEcommerce.Models
{
    public enum RFQStatus
    {
        Draft,
        Open,
        Published,
        Responding,
        Awarded,
        Expired,
        Cancelled
    }

    public enum MessageType
    {
        General,
        Negotiation,
        Dispute,
        Support,
        System
    }

    public enum DisputeStatus
    {
        Open,
        UnderReview,
        Resolved,
        Closed
    }

    public enum DisputeType
    {
        Quality,
        Delivery,
        Payment,
        Communication,
        Other
    }

    public enum LogisticsType
    {
        Air,
        Sea,
        Land,
        Express
    }

    public class SupplierProfile
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        
        // Business Information
        public string CompanyName { get; set; }
        public string? BusinessLicense { get; set; }
        public string? ISO_Certification { get; set; }
        public string? QualityCertification { get; set; }
        public int? EmployeeCount { get; set; }
        public decimal? AnnualRevenue { get; set; }
        public string? RevenueCurrency { get; set; } = "USD";
        
        // Manufacturing Capabilities
        public string? ManufacturingCapacity { get; set; }
        public string? ProductionLeadTime { get; set; }
        public string? QualityControlProcess { get; set; }
        public string? Certifications { get; set; } // JSON array
        
        // Trade Information
        public string[]? ExportMarkets { get; set; }
        public string[]? PaymentTerms { get; set; }
        public string[]? ShippingMethods { get; set; }
        public string? MinimumOrderValue { get; set; }
        public string? SamplePolicy { get; set; }
        
        // Contact Information
        public string? ContactPerson { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? Website { get; set; }
        
        // Analytics
        public int TotalProducts { get; set; } = 0;
        public int TotalOrders { get; set; } = 0;
        public decimal TotalRevenue { get; set; } = 0;
        public decimal Rating { get; set; } = 0;
        public int ReviewCount { get; set; } = 0;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }

    public class BuyerProfile
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        
        // Business Information
        public string CompanyName { get; set; }
        public string? Industry { get; set; }
        public string? BusinessType { get; set; }
        public int? EmployeeCount { get; set; }
        public decimal? AnnualPurchasingBudget { get; set; }
        public string? BudgetCurrency { get; set; } = "USD";
        
        // Purchasing Preferences
        public string[]? PreferredCategories { get; set; }
        public string[]? PreferredCountries { get; set; }
        public string[]? PaymentMethods { get; set; }
        public string? PreferredShippingMethod { get; set; }
        public string? MinimumOrderValue { get; set; }
        
        // Analytics
        public int TotalOrders { get; set; } = 0;
        public decimal TotalSpent { get; set; } = 0;
        public int ActiveRFQs { get; set; } = 0;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }

    public class RFQ
    {
        public int Id { get; set; }
        public string BuyerId { get; set; }
        public virtual ApplicationUser Buyer { get; set; }
        
        // RFQ Details
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string? SubCategory { get; set; }
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal? Budget { get; set; }
        public string? BudgetCurrency { get; set; } = "USD";
        
        // Requirements
        public string? Specifications { get; set; } // JSON object
        public string? QualityRequirements { get; set; }
        public string? Certifications { get; set; } // JSON array
        public string? PackagingRequirements { get; set; }
        public string? DeliveryRequirements { get; set; }
        
        // Timeline
        public DateTime? Deadline { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public RFQStatus Status { get; set; } = RFQStatus.Draft;
        
        // Analytics
        public int ViewCount { get; set; } = 0;
        public int ResponseCount { get; set; } = 0;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual ICollection<RFQItem> RFQItems { get; set; }
        public virtual ICollection<RFQResponse> Responses { get; set; }
    }

    public class RFQItem
    {
        public int Id { get; set; }
        public int RFQId { get; set; }
        public virtual RFQ RFQ { get; set; }
        public int? ProductId { get; set; }
        public virtual Product Product { get; set; }
        
        public string ItemName { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Specifications { get; set; } // JSON object
    }

    public class RFQResponse
    {
        public int Id { get; set; }
        public int RFQId { get; set; }
        public virtual RFQ RFQ { get; set; }
        public string SupplierId { get; set; }
        public virtual ApplicationUser Supplier { get; set; }
        
        // Response Details
        public decimal Price { get; set; }
        public string? PriceCurrency { get; set; } = "USD";
        public string? UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        
        // Delivery Information
        public int? LeadTimeDays { get; set; }
        public string? DeliveryTerms { get; set; }
        public string? PaymentTerms { get; set; }
        
        // Additional Information
        public string? Message { get; set; }
        public string? Specifications { get; set; } // JSON object
        public string? Certifications { get; set; } // JSON array
        public string? SamplePolicy { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }

    public class Message
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public virtual ApplicationUser Sender { get; set; }
        public string ReceiverId { get; set; }
        public virtual ApplicationUser Receiver { get; set; }
        
        public string Subject { get; set; }
        public string Content { get; set; }
        public MessageType Type { get; set; } = MessageType.General;
        
        // Related Entities
        public int? ProductId { get; set; }
        public virtual Product Product { get; set; }
        public int? OrderId { get; set; }
        public virtual Order Order { get; set; }
        public int? RFQId { get; set; }
        public virtual RFQ RFQ { get; set; }
        public int? DisputeId { get; set; }
        public virtual Dispute Dispute { get; set; }
        
        // Message Status
        public bool IsRead { get; set; } = false;
        public DateTime? ReadDate { get; set; }
        public bool IsEncrypted { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class Dispute
    {
        public int Id { get; set; }
        public string InitiatorId { get; set; }
        public virtual ApplicationUser Initiator { get; set; }
        public string RespondentId { get; set; }
        public virtual ApplicationUser Respondent { get; set; }
        
        // Dispute Details
        public string Title { get; set; }
        public string Description { get; set; }
        public DisputeType Type { get; set; }
        public DisputeStatus Status { get; set; } = DisputeStatus.Open;
        
        // Related Entities
        public int? OrderId { get; set; }
        public virtual Order Order { get; set; }
        public int? ProductId { get; set; }
        public virtual Product Product { get; set; }
        
        // Resolution
        public string? Resolution { get; set; }
        public string? ResolvedBy { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public decimal? RefundAmount { get; set; }
        public string? RefundCurrency { get; set; } = "USD";
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual ICollection<Message> Messages { get; set; }
    }

    public class LogisticsCalculator
    {
        public int Id { get; set; }
        public string FromCountry { get; set; }
        public string FromCity { get; set; }
        public string ToCountry { get; set; }
        public string ToCity { get; set; }
        
        public LogisticsType Type { get; set; }
        public decimal Weight { get; set; }
        public string WeightUnit { get; set; } = "kg";
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string DimensionUnit { get; set; } = "cm";
        
        public decimal EstimatedCost { get; set; }
        public string CostCurrency { get; set; } = "USD";
        public int EstimatedDays { get; set; }
        public string? Carrier { get; set; }
        public string? ServiceLevel { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class LogisticsRequest
    {
        public string OriginCountry { get; set; }
        public string OriginCity { get; set; }
        public string OriginPostalCode { get; set; }
        public string DestinationCountry { get; set; }
        public string DestinationCity { get; set; }
        public string DestinationPostalCode { get; set; }
        
        public decimal Weight { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string PackageType { get; set; } = "Box";
        public decimal? DeclaredValue { get; set; }
        public string Currency { get; set; } = "USD";
        
        public string ShippingMethod { get; set; }
        public string ServiceLevel { get; set; }
    }

    public class EscrowTransaction
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }
        
        public string BuyerId { get; set; }
        public virtual ApplicationUser Buyer { get; set; }
        public string SupplierId { get; set; }
        public virtual ApplicationUser Supplier { get; set; }
        
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = "Pending"; // Pending, Funded, Released, Refunded
        public DateTime? FundedDate { get; set; }
        public DateTime? ReleasedDate { get; set; }
        public DateTime? RefundedDate { get; set; }
        
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }

    public class TradeAssurance
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }
        
        public string BuyerId { get; set; }
        public virtual ApplicationUser Buyer { get; set; }
        public string SupplierId { get; set; }
        public virtual ApplicationUser Supplier { get; set; }
        
        public decimal CoverageAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = "Active"; // Active, Claimed, Resolved, Expired
        public DateTime? ExpiryDate { get; set; }
        public DateTime? ClaimedDate { get; set; }
        public DateTime? ResolvedDate { get; set; }
        
        public string? ClaimReason { get; set; }
        public string? Resolution { get; set; }
        public decimal? PayoutAmount { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
} 