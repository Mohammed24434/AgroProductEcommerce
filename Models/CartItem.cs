namespace AgroProductEcommerce.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public string CartId { get; set; } // Session ID for anonymous users
        
        // Enhanced fields for B2B functionality
        public decimal UnitPrice { get; set; } // Price at time of adding to cart
        public string Notes { get; set; } // Additional information (bulk pricing, quality, etc.)
        public DateTime AddedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public bool IsSavedForLater { get; set; } = false;
        
        // B2B specific fields
        public bool IsBulkOrder { get; set; } = false;
        public string SpecialInstructions { get; set; }
        public DateTime? RequestedDeliveryDate { get; set; }
    }
}