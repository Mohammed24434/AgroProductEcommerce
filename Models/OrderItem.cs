using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroProductEcommerce.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        
        // Product Information
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }
        public int OrderId { get; set; }
        public virtual Order Order { get; set; }
        
        // Quantity and Pricing
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; } = "USD";
        
        // B2B Features
        public string? SKU { get; set; }
        public string? ProductName { get; set; } // Snapshot of product name at time of order
        public string? ProductDescription { get; set; }
        public string? ProductImageUrl { get; set; }
        
        // Supplier Information
        public string? SupplierId { get; set; }
        public virtual ApplicationUser Supplier { get; set; }
        public string? SupplierName { get; set; }
        
        // Quality and Specifications
        public string? Specifications { get; set; } // JSON object
        public string? QualityGrade { get; set; }
        public string? Certifications { get; set; } // JSON array
        public string? CountryOfOrigin { get; set; }
        
        // Shipping and Logistics
        public decimal? Weight { get; set; }
        public string? WeightUnit { get; set; } = "kg";
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string? DimensionUnit { get; set; } = "cm";
        
        // Inventory Tracking
        public int? ReservedQuantity { get; set; }
        public bool IsBackordered { get; set; } = false;
        public DateTime? ExpectedRestockDate { get; set; }
        
        // Timestamps
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}
