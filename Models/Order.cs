using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroProductEcommerce.Models
{
    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Processing,
        Shipped,
        Delivered,
        Cancelled,
        Refunded,
        Disputed
    }

    public enum PaymentStatus
    {
        Pending,
        Authorized,
        Paid,
        Failed,
        Refunded,
        PartiallyRefunded
    }

    public enum PaymentMethod
    {
        CreditCard,
        BankTransfer,
        PayPal,
        Escrow,
        TradeAssurance,
        LetterOfCredit,
        CashOnDelivery
    }

    public enum ShippingMethod
    {
        Standard,
        Express,
        Air,
        Sea,
        Land,
        Pickup
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        // Order Information
        public string OrderNumber { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ConfirmedDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        
        // Customer Information
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        
        // Billing Address
        public string BillingAddress { get; set; }
        public string BillingCity { get; set; }
        public string BillingState { get; set; }
        public string BillingPostalCode { get; set; }
        public string BillingCountry { get; set; }
        
        // Shipping Address
        public string ShippingAddress { get; set; }
        public string ShippingCity { get; set; }
        public string ShippingState { get; set; }
        public string ShippingPostalCode { get; set; }
        public string ShippingCountry { get; set; }
        public string? ShippingInstructions { get; set; }
        
        // Financial Information
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal? ExchangeRate { get; set; }
        
        // Payment Information
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public string? TransactionId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentGateway { get; set; }
        
        // Shipping Information
        public ShippingMethod ShippingMethod { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
        
        // Order Status
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string? StatusNotes { get; set; }
        
        // B2B Features
        public bool IsB2B { get; set; } = false;
        public string? PurchaseOrderNumber { get; set; }
        public string? ContractNumber { get; set; }
        public bool TradeAssuranceEnabled { get; set; } = false;
        public decimal? TradeAssuranceAmount { get; set; }
        public DateTime? TradeAssuranceExpiryDate { get; set; }
        
        // Escrow Information
        public bool EscrowEnabled { get; set; } = false;
        public string? EscrowId { get; set; }
        public DateTime? EscrowReleaseDate { get; set; }
        public string? EscrowStatus { get; set; }
        
        // International Trade
        public string? CustomsDeclaration { get; set; }
        public string? HS_Codes { get; set; }
        public decimal? CustomsValue { get; set; }
        public string? CountryOfOrigin { get; set; }
        
        // Analytics
        public int ItemCount { get; set; }
        public decimal? ProfitMargin { get; set; }
        public string? SalesChannel { get; set; }
        
        // Timestamps
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<Message> Messages { get; set; }
        public virtual ICollection<Dispute> Disputes { get; set; }
    }
}