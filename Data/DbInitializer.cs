using AgroProductEcommerce.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AgroProductEcommerce.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Check if database exists and apply any pending migrations
            context.Database.EnsureCreated();

            // Seed roles
            string[] roles = { "Admin", "Supplier", "Buyer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed admin user
            var adminEmail = "admin@globaltradehub.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "GlobalTrade",
                    LastName = "Admin",
                    EmailConfirmed = true,
                    PhoneNumber = "+1-555-ADMIN-01",
                    PhoneNumberConfirmed = true,
                    UserType = UserType.Admin,
                    KYCStatus = KYCStatus.Approved,
                    CompanyName = "GlobalTrade Hub Platform",
                    BusinessType = BusinessType.Corporation,
                    Address = "123 Global Trade Center",
                    City = "New York",
                    State = "NY",
                    PostalCode = "10001",
                    Country = "United States",
                    PreferredCurrency = "USD",
                    TradeAssuranceEnabled = true,
                    TradeAssuranceLimit = 1000000,
                    DataProcessingConsent = true,
                    DataProcessingConsentDate = DateTime.UtcNow,
                    MarketingConsent = true,
                    MarketingConsentDate = DateTime.UtcNow,
                    RegistrationDate = DateTime.UtcNow,
                    LastLoginDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@GlobalTrade2024!");

                if (!result.Succeeded)
                {
                    throw new Exception("Failed to create admin user: " +
                                       string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                var addToRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                if (!addToRoleResult.Succeeded)
                {
                    throw new Exception("Failed to add admin user to role: " +
                                       string.Join(", ", addToRoleResult.Errors.Select(e => e.Description)));
                }
            }

            // Seed sample supplier user
            var supplierEmail = "supplier@globaltradehub.com";
            var supplierUser = await userManager.FindByEmailAsync(supplierEmail);

            if (supplierUser == null)
            {
                supplierUser = new ApplicationUser
                {
                    UserName = supplierEmail,
                    Email = supplierEmail,
                    FirstName = "Premium",
                    LastName = "Supplier",
                    EmailConfirmed = true,
                    PhoneNumber = "+1-555-SUPPLY-01",
                    PhoneNumberConfirmed = true,
                    UserType = UserType.Supplier,
                    KYCStatus = KYCStatus.Approved,
                    CompanyName = "Premium Agricultural Exports Ltd",
                    BusinessType = BusinessType.Manufacturer,
                    Address = "456 Farm Road",
                    City = "California",
                    State = "CA",
                    PostalCode = "90210",
                    Country = "United States",
                    TaxIdentificationNumber = "TAX123456789",
                    Website = "https://premiumagri.com",
                    BusinessDescription = "Premium agricultural products exporter with 15+ years experience",
                    YearsInBusiness = 15,
                    Industry = "Agriculture",
                    PreferredCurrency = "USD",
                    TradeAssuranceEnabled = true,
                    TradeAssuranceLimit = 500000,
                    DataProcessingConsent = true,
                    DataProcessingConsentDate = DateTime.UtcNow,
                    MarketingConsent = true,
                    MarketingConsentDate = DateTime.UtcNow,
                    RegistrationDate = DateTime.UtcNow,
                    LastLoginDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(supplierUser, "Supplier@GlobalTrade2024!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(supplierUser, "Supplier");
                }
            }

            // Seed sample buyer user
            var buyerEmail = "buyer@globaltradehub.com";
            var buyerUser = await userManager.FindByEmailAsync(buyerEmail);

            if (buyerUser == null)
            {
                buyerUser = new ApplicationUser
                {
                    UserName = buyerEmail,
                    Email = buyerEmail,
                    FirstName = "Global",
                    LastName = "Buyer",
                    EmailConfirmed = true,
                    PhoneNumber = "+1-555-BUYER-01",
                    PhoneNumberConfirmed = true,
                    UserType = UserType.Buyer,
                    KYCStatus = KYCStatus.Approved,
                    CompanyName = "Global Import Solutions",
                    BusinessType = BusinessType.Distributor,
                    Address = "789 Import Street",
                    City = "Chicago",
                    State = "IL",
                    PostalCode = "60601",
                    Country = "United States",
                    TaxIdentificationNumber = "TAX987654321",
                    Website = "https://globalimports.com",
                    BusinessDescription = "Leading importer of agricultural products for European markets",
                    YearsInBusiness = 12,
                    Industry = "Import/Export",
                    PreferredCurrency = "EUR",
                    TradeAssuranceEnabled = true,
                    TradeAssuranceLimit = 750000,
                    DataProcessingConsent = true,
                    DataProcessingConsentDate = DateTime.UtcNow,
                    MarketingConsent = true,
                    MarketingConsentDate = DateTime.UtcNow,
                    RegistrationDate = DateTime.UtcNow,
                    LastLoginDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(buyerUser, "Buyer@GlobalTrade2024!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(buyerUser, "Buyer");
                }
            }

            // Seed products if none exist
            if (!context.Products.Any())
            {
                var products = new Product[]
                {
                    new Product
                    {
                        Name = "Organic Avocado Premium Grade",
                        Description = "Premium organic avocados sourced from sustainable farms. Perfect for export markets with high quality standards.",
                        Price = 24.99m,
                        ImageUrl = "/images/Avocado.jpg",
                        Category = "Fresh",
                        SubCategory = "Fruits",
                        Brand = "Organic Valley",
                        SKU = "AVO-ORG-001",
                        StockQuantity = 5000,
                        MinimumOrderQuantity = 100,
                        Weight = 0.5m,
                        WeightUnit = "kg",
                        Length = 15.0m,
                        Width = 10.0m,
                        Height = 8.0m,
                        DimensionUnit = "cm",
                        HS_Code = "08044000",
                        CountryOfOrigin = "Mexico",
                        SupplierId = supplierUser?.Id,
                        Status = ProductStatus.Active,
                        ProductType = ProductType.Physical,
                        TradeAssuranceEligible = true,
                        QualityGrade = "Premium",
                        Warranty = "7 days",
                        ReturnPolicy = "7 days return policy for quality issues",
                        AI_Tags = "organic, premium, sustainable, export-grade",
                        ProductFeatures = "Organic certified, Sustainable farming, Export quality, Rich in healthy fats",
                        Specifications = "Size: Medium to Large, Color: Dark green, Texture: Firm, Ripeness: Ready to eat",
                        Certifications = "USDA Organic, Fair Trade, Global GAP",
                        SearchKeywords = "avocado organic premium fresh fruit healthy",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "Premium Basmati Rice",
                        Description = "Aromatic long-grain basmati rice with exceptional quality. Ideal for bulk export and restaurant supply.",
                        Price = 59.99m,
                        ImageUrl = "/images/BasmatiGreen2.jpg",
                        Category = "Dried",
                        SubCategory = "Grains",
                        Brand = "Royal Basmati",
                        SKU = "RICE-BAS-001",
                        StockQuantity = 10000,
                        MinimumOrderQuantity = 500,
                        Weight = 1.0m,
                        WeightUnit = "kg",
                        Length = 20.0m,
                        Width = 15.0m,
                        Height = 10.0m,
                        DimensionUnit = "cm",
                        HS_Code = "10063000",
                        CountryOfOrigin = "India",
                        SupplierId = supplierUser?.Id,
                        Status = ProductStatus.Active,
                        ProductType = ProductType.Physical,
                        TradeAssuranceEligible = true,
                        QualityGrade = "Premium",
                        Warranty = "1 year",
                        ReturnPolicy = "Quality guarantee for 1 year",
                        AI_Tags = "basmati rice premium aromatic long-grain",
                        ProductFeatures = "Aromatic, Long grain, Premium quality, Aged rice",
                        Specifications = "Length: Extra long, Color: White, Texture: Non-sticky, Aging: 2 years",
                        Certifications = "ISO 22000, HACCP, FSSAI",
                        SearchKeywords = "basmati rice premium aromatic long grain",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "Natural Henna Powder",
                        Description = "Pure natural henna powder for hair coloring and body art. Sourced from organic farms.",
                        Price = 39.99m,
                        ImageUrl = "/images/Henna2.jpg",
                        Category = "Herbs",
                        SubCategory = "Natural Dyes",
                        Brand = "Natural Henna Co",
                        SKU = "HENNA-NAT-001",
                        StockQuantity = 3000,
                        MinimumOrderQuantity = 50,
                        Weight = 0.25m,
                        WeightUnit = "kg",
                        Length = 12.0m,
                        Width = 8.0m,
                        Height = 5.0m,
                        DimensionUnit = "cm",
                        HS_Code = "14049000",
                        CountryOfOrigin = "Morocco",
                        SupplierId = supplierUser?.Id,
                        Status = ProductStatus.Active,
                        ProductType = ProductType.Physical,
                        TradeAssuranceEligible = true,
                        QualityGrade = "Premium",
                        Warranty = "2 years",
                        ReturnPolicy = "Quality guarantee for 2 years",
                        AI_Tags = "henna natural organic hair dye body art",
                        ProductFeatures = "Pure natural, Organic certified, No chemicals, Safe for skin",
                        Specifications = "Color: Natural brown, Purity: 100%, Mesh size: 200, Origin: Morocco",
                        Certifications = "Organic certified, ISO 9001, Halal certified",
                        SearchKeywords = "henna natural organic hair dye body art",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "Qasil Natural Face Mask",
                        Description = "Traditional Somali qasil powder for natural skincare. Rich in antioxidants and minerals.",
                        Price = 29.99m,
                        ImageUrl = "/images/qasil.jpg",
                        Category = "Herbs",
                        SubCategory = "Skincare",
                        Brand = "Somali Beauty",
                        SKU = "QASIL-NAT-001",
                        StockQuantity = 2000,
                        MinimumOrderQuantity = 25,
                        Weight = 0.1m,
                        WeightUnit = "kg",
                        Length = 10.0m,
                        Width = 6.0m,
                        Height = 3.0m,
                        DimensionUnit = "cm",
                        HS_Code = "33043000",
                        CountryOfOrigin = "Somalia",
                        SupplierId = supplierUser?.Id,
                        Status = ProductStatus.Active,
                        ProductType = ProductType.Physical,
                        TradeAssuranceEligible = true,
                        QualityGrade = "Premium",
                        Warranty = "1 year",
                        ReturnPolicy = "Quality guarantee for 1 year",
                        AI_Tags = "qasil natural skincare face mask traditional",
                        ProductFeatures = "Natural skincare, Traditional recipe, Rich in minerals, Antioxidant properties",
                        Specifications = "Color: Natural green, Texture: Fine powder, Origin: Somalia, Traditional processing",
                        Certifications = "Natural certified, Traditional knowledge, ISO 22716",
                        SearchKeywords = "qasil natural skincare face mask traditional somali",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    }
                };

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }
        }
    }
}