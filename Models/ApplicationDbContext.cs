using System.Collections.Generic;
using AgroProductEcommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AgroProductEcommerce.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Core E-commerce Models
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        // B2B Models
        public DbSet<SupplierProfile> SupplierProfiles { get; set; }
        public DbSet<BuyerProfile> BuyerProfiles { get; set; }
        public DbSet<RFQ> RFQs { get; set; }
        public DbSet<RFQItem> RFQItems { get; set; }
        public DbSet<RFQResponse> RFQResponses { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Dispute> Disputes { get; set; }
        public DbSet<LogisticsCalculator> LogisticsCalculators { get; set; }

        // Payment and Logistics Models
        public DbSet<EscrowTransaction> EscrowTransactions { get; set; }
        public DbSet<TradeAssurance> TradeAssurances { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser
            builder.Entity<ApplicationUser>()
                .Property(u => u.UserType)
                .HasDefaultValue(UserType.Buyer);

            builder.Entity<ApplicationUser>()
                .Property(u => u.KYCStatus)
                .HasDefaultValue(KYCStatus.Pending);

            // Configure Product
            builder.Entity<Product>()
                .Property(p => p.Status)
                .HasDefaultValue(ProductStatus.Active);

            builder.Entity<Product>()
                .Property(p => p.ProductType)
                .HasDefaultValue(ProductType.Physical);

            builder.Entity<Product>()
                .Property(p => p.TradeAssuranceEligible)
                .HasDefaultValue(true);

            // Configure Order
            builder.Entity<Order>()
                .Property(o => o.Status)
                .HasDefaultValue(OrderStatus.Pending);

            builder.Entity<Order>()
                .Property(o => o.PaymentStatus)
                .HasDefaultValue(PaymentStatus.Pending);

            builder.Entity<Order>()
                .Property(o => o.Currency)
                .HasDefaultValue("USD");

            // Configure RFQ
            builder.Entity<RFQ>()
                .Property(r => r.Status)
                .HasDefaultValue(RFQStatus.Draft);

            builder.Entity<RFQ>()
                .Property(r => r.BudgetCurrency)
                .HasDefaultValue("USD");

            // Configure Message
            builder.Entity<Message>()
                .Property(m => m.Type)
                .HasDefaultValue(MessageType.General);

            builder.Entity<Message>()
                .Property(m => m.IsEncrypted)
                .HasDefaultValue(true);

            // Configure Dispute
            builder.Entity<Dispute>()
                .Property(d => d.Status)
                .HasDefaultValue(DisputeStatus.Open);

            // Configure LogisticsCalculator
            builder.Entity<LogisticsCalculator>()
                .Property(l => l.WeightUnit)
                .HasDefaultValue("kg");

            builder.Entity<LogisticsCalculator>()
                .Property(l => l.DimensionUnit)
                .HasDefaultValue("cm");

            builder.Entity<LogisticsCalculator>()
                .Property(l => l.CostCurrency)
                .HasDefaultValue("USD");

            // Configure relationships
            builder.Entity<Product>()
                .HasOne(p => p.Supplier)
                .WithMany()
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Supplier)
                .WithMany()
                .HasForeignKey(oi => oi.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RFQ>()
                .HasOne(r => r.Buyer)
                .WithMany(u => u.CreatedRFQs)
                .HasForeignKey(r => r.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RFQResponse>()
                .HasOne(rr => rr.Supplier)
                .WithMany(u => u.RFQResponses)
                .HasForeignKey(rr => rr.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Dispute>()
                .HasOne(d => d.Initiator)
                .WithMany(u => u.Disputes)
                .HasForeignKey(d => d.InitiatorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Dispute>()
                .HasOne(d => d.Respondent)
                .WithMany(u => u.ResolvedDisputes)
                .HasForeignKey(d => d.RespondentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure JSON properties
            builder.Entity<Product>()
                .Property(p => p.AI_Tags)
                .HasColumnType("nvarchar(max)");

            builder.Entity<Product>()
                .Property(p => p.ProductFeatures)
                .HasColumnType("nvarchar(max)");

            builder.Entity<Product>()
                .Property(p => p.Specifications)
                .HasColumnType("nvarchar(max)");

            builder.Entity<Product>()
                .Property(p => p.Certifications)
                .HasColumnType("nvarchar(max)");

            builder.Entity<SupplierProfile>()
                .Property(sp => sp.ExportMarkets)
                .HasColumnType("nvarchar(max)");

            builder.Entity<SupplierProfile>()
                .Property(sp => sp.PaymentTerms)
                .HasColumnType("nvarchar(max)");

            builder.Entity<SupplierProfile>()
                .Property(sp => sp.ShippingMethods)
                .HasColumnType("nvarchar(max)");

            builder.Entity<SupplierProfile>()
                .Property(sp => sp.Certifications)
                .HasColumnType("nvarchar(max)");

            builder.Entity<BuyerProfile>()
                .Property(bp => bp.PreferredCategories)
                .HasColumnType("nvarchar(max)");

            builder.Entity<BuyerProfile>()
                .Property(bp => bp.PreferredCountries)
                .HasColumnType("nvarchar(max)");

            builder.Entity<BuyerProfile>()
                .Property(bp => bp.PaymentMethods)
                .HasColumnType("nvarchar(max)");

            builder.Entity<RFQ>()
                .Property(r => r.Specifications)
                .HasColumnType("nvarchar(max)");

            builder.Entity<RFQ>()
                .Property(r => r.Certifications)
                .HasColumnType("nvarchar(max)");

            builder.Entity<RFQItem>()
                .Property(ri => ri.Specifications)
                .HasColumnType("nvarchar(max)");

            builder.Entity<RFQResponse>()
                .Property(rr => rr.Specifications)
                .HasColumnType("nvarchar(max)");

            builder.Entity<RFQResponse>()
                .Property(rr => rr.Certifications)
                .HasColumnType("nvarchar(max)");

            builder.Entity<OrderItem>()
                .Property(oi => oi.Specifications)
                .HasColumnType("nvarchar(max)");

            builder.Entity<OrderItem>()
                .Property(oi => oi.Certifications)
                .HasColumnType("nvarchar(max)");

            // Configure EscrowTransaction foreign keys
            builder.Entity<EscrowTransaction>()
                .HasOne(et => et.Buyer)
                .WithMany()
                .HasForeignKey(et => et.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<EscrowTransaction>()
                .HasOne(et => et.Supplier)
                .WithMany()
                .HasForeignKey(et => et.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<EscrowTransaction>()
                .HasOne(et => et.Order)
                .WithMany()
                .HasForeignKey(et => et.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure TradeAssurance foreign keys
            builder.Entity<TradeAssurance>()
                .HasOne(ta => ta.Buyer)
                .WithMany()
                .HasForeignKey(ta => ta.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TradeAssurance>()
                .HasOne(ta => ta.Supplier)
                .WithMany()
                .HasForeignKey(ta => ta.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TradeAssurance>()
                .HasOne(ta => ta.Order)
                .WithMany()
                .HasForeignKey(ta => ta.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}