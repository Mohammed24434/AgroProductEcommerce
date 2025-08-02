using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgroProductEcommerce.Models
{
    public enum ProductStatus
    {
        Active,
        Inactive,
        OutOfStock,
        Discontinued,
        PendingApproval
    }

    public enum ProductType
    {
        Physical,
        Digital,
        Service
    }

    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // B2B Pricing
        [Column(TypeName = "decimal(18,2)")]
        public decimal? BulkPrice { get; set; }
        public int? BulkQuantity { get; set; }
        public string? PricingTier { get; set; } // e.g., "1-10", "11-50", "51+"
        
        // Supplier Information
        public string SupplierId { get; set; }
        public virtual ApplicationUser Supplier { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierLocation { get; set; }
        
        // Product Details
        public string ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public string Category { get; set; }
        public string? SubCategory { get; set; }
        public string Size { get; set; }
        public string Color { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? SKU { get; set; }
        public string? UPC { get; set; }
        
        // Inventory Management
        public int StockQuantity { get; set; }
        public int? MinimumOrderQuantity { get; set; } = 1;
        public int? MaximumOrderQuantity { get; set; }
        public int? ReservedQuantity { get; set; } = 0;
        public bool AllowBackorders { get; set; } = false;
        public int? LeadTimeDays { get; set; }
        
        // Product Status and Type
        public ProductStatus Status { get; set; } = ProductStatus.Active;
        public ProductType ProductType { get; set; } = ProductType.Physical;
        
        // International Trade
        public string? CountryOfOrigin { get; set; }
        public string? HS_Code { get; set; }
        public decimal? Weight { get; set; }
        public string? WeightUnit { get; set; } = "kg";
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string? DimensionUnit { get; set; } = "cm";
        
        // AI and Search Optimization
        public string? AI_Tags { get; set; } // JSON array of AI-generated tags
        public string? SearchKeywords { get; set; }
        public string? ProductFeatures { get; set; } // JSON array of features
        public string? Specifications { get; set; } // JSON object
        public string? Certifications { get; set; } // JSON array
        
        // Quality and Assurance
        public bool TradeAssuranceEligible { get; set; } = true;
        public string? QualityGrade { get; set; }
        public string? Warranty { get; set; }
        public string? ReturnPolicy { get; set; }
        
        // Analytics
        public int ViewCount { get; set; } = 0;
        public int OrderCount { get; set; } = 0;
        public decimal Rating { get; set; } = 0;
        public int ReviewCount { get; set; } = 0;
        
        // Timestamps
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedDate { get; set; }
        
        // Navigation Properties
        public virtual ICollection<ProductImage> ProductImages { get; set; }
        public virtual ICollection<ProductReview> Reviews { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual ICollection<RFQItem> RFQItems { get; set; }
    }

    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }
        public string ImageUrl { get; set; }
        public string? AltText { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public bool IsPrimary { get; set; } = false;
    }

    public class ProductReview
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsVerified { get; set; } = false;
    }
}