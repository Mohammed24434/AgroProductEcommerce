using System.Text.Json;
using AgroProductEcommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace AgroProductEcommerce.Services
{
    public interface IAIService
    {
        Task<List<string>> GenerateProductTagsAsync(Product product);
        Task<List<Product>> GetPersonalizedRecommendationsAsync(string userId, int limit = 10);
        Task<List<Product>> SearchProductsWithAIAsync(string query, string? userId = null, int limit = 20);
        Task<List<Product>> GetSimilarProductsAsync(int productId, int limit = 5);
        Task<Dictionary<string, object>> AnalyzeProductImageAsync(string imageUrl);
        Task<List<string>> ExtractProductFeaturesAsync(string description);
        Task<decimal> PredictProductDemandAsync(int productId);
        Task<List<string>> SuggestSearchKeywordsAsync(string category);
    }

    public class AIService : IAIService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AIService> _logger;
        private readonly HttpClient _httpClient;

        public AIService(ApplicationDbContext context, ILogger<AIService> logger, HttpClient httpClient)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<List<string>> GenerateProductTagsAsync(Product product)
        {
            try
            {
                // Simulate AI tag generation based on product attributes
                var tags = new List<string>();
                
                // Category-based tags
                if (!string.IsNullOrEmpty(product.Category))
                {
                    tags.Add(product.Category.ToLower());
                }
                
                if (!string.IsNullOrEmpty(product.SubCategory))
                {
                    tags.Add(product.SubCategory.ToLower());
                }

                // Brand-based tags
                if (!string.IsNullOrEmpty(product.Brand))
                {
                    tags.Add(product.Brand.ToLower());
                }

                // Quality-based tags
                if (!string.IsNullOrEmpty(product.QualityGrade))
                {
                    tags.Add(product.QualityGrade.ToLower());
                }

                // Origin-based tags
                if (!string.IsNullOrEmpty(product.CountryOfOrigin))
                {
                    tags.Add(product.CountryOfOrigin.ToLower());
                }

                // Price-based tags
                if (product.BulkPrice.HasValue && product.BulkPrice < product.Price)
                {
                    tags.Add("bulk-discount");
                }

                if (product.TradeAssuranceEligible)
                {
                    tags.Add("trade-assurance");
                }

                // AI-generated tags based on description analysis
                if (!string.IsNullOrEmpty(product.Description))
                {
                    var descriptionTags = await ExtractProductFeaturesAsync(product.Description);
                    tags.AddRange(descriptionTags);
                }

                return tags.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating product tags for product {ProductId}", product.Id);
                return new List<string>();
            }
        }

        public async Task<List<Product>> GetPersonalizedRecommendationsAsync(string userId, int limit = 10)
        {
            try
            {
                // Get user's order history and preferences
                var userOrders = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(50)
                    .ToListAsync();

                var userPreferences = new Dictionary<string, int>();
                
                // Analyze user's purchase history
                foreach (var order in userOrders)
                {
                    foreach (var item in order.OrderItems)
                    {
                        var category = item.Product?.Category?.ToLower();
                        if (!string.IsNullOrEmpty(category))
                        {
                            userPreferences[category] = userPreferences.GetValueOrDefault(category, 0) + item.Quantity;
                        }
                    }
                }

                // Get products based on user preferences
                var recommendedProducts = new List<Product>();
                
                if (userPreferences.Any())
                {
                    var topCategories = userPreferences.OrderByDescending(x => x.Value).Take(3).Select(x => x.Key);
                    
                    recommendedProducts = await _context.Products
                        .Where(p => p.Status == ProductStatus.Active && 
                                   topCategories.Contains(p.Category.ToLower()) &&
                                   p.StockQuantity > 0)
                        .OrderByDescending(p => p.Rating)
                        .ThenByDescending(p => p.ViewCount)
                        .Take(limit)
                        .ToListAsync();
                }

                // If not enough recommendations, add popular products
                if (recommendedProducts.Count < limit)
                {
                    var popularProducts = await _context.Products
                        .Where(p => p.Status == ProductStatus.Active && 
                                   p.StockQuantity > 0 &&
                                   !recommendedProducts.Select(rp => rp.Id).Contains(p.Id))
                        .OrderByDescending(p => p.ViewCount)
                        .ThenByDescending(p => p.Rating)
                        .Take(limit - recommendedProducts.Count)
                        .ToListAsync();

                    recommendedProducts.AddRange(popularProducts);
                }

                return recommendedProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personalized recommendations for user {UserId}", userId);
                return new List<Product>();
            }
        }

        public async Task<List<Product>> SearchProductsWithAIAsync(string query, string? userId = null, int limit = 20)
        {
            try
            {
                var searchTerms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var products = new List<Product>();

                // Search by product name, description, and AI tags
                var baseQuery = _context.Products
                    .Where(p => p.Status == ProductStatus.Active && p.StockQuantity > 0);

                foreach (var term in searchTerms)
                {
                    baseQuery = baseQuery.Where(p => 
                        p.Name.ToLower().Contains(term) ||
                        p.Description.ToLower().Contains(term) ||
                        p.Category.ToLower().Contains(term) ||
                        p.SubCategory.ToLower().Contains(term) ||
                        p.Brand.ToLower().Contains(term) ||
                        p.AI_Tags.ToLower().Contains(term) ||
                        p.SearchKeywords.ToLower().Contains(term));
                }

                products = await baseQuery
                    .OrderByDescending(p => p.Rating)
                    .ThenByDescending(p => p.ViewCount)
                    .Take(limit)
                    .ToListAsync();

                // If user is logged in, personalize results
                if (!string.IsNullOrEmpty(userId) && products.Any())
                {
                    var userPreferences = await GetUserPreferencesAsync(userId);
                    if (userPreferences.Any())
                    {
                        // Boost products that match user preferences
                        products = products.OrderByDescending(p => 
                            userPreferences.Contains(p.Category.ToLower()) ? 1 : 0)
                            .ThenByDescending(p => p.Rating)
                            .ToList();
                    }
                }

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with AI for query: {Query}", query);
                return new List<Product>();
            }
        }

        public async Task<List<Product>> GetSimilarProductsAsync(int productId, int limit = 5)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return new List<Product>();

                var similarProducts = await _context.Products
                    .Where(p => p.Id != productId &&
                               p.Status == ProductStatus.Active &&
                               p.StockQuantity > 0 &&
                               (p.Category == product.Category ||
                                p.SubCategory == product.SubCategory ||
                                p.Brand == product.Brand))
                    .OrderByDescending(p => p.Rating)
                    .ThenByDescending(p => p.ViewCount)
                    .Take(limit)
                    .ToListAsync();

                return similarProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting similar products for product {ProductId}", productId);
                return new List<Product>();
            }
        }

        public async Task<Dictionary<string, object>> AnalyzeProductImageAsync(string imageUrl)
        {
            try
            {
                // Simulate AI image analysis
                var analysis = new Dictionary<string, object>
                {
                    ["colors"] = new[] { "green", "brown", "natural" },
                    ["materials"] = new[] { "organic", "natural" },
                    ["quality_score"] = 0.85,
                    ["product_type"] = "agricultural",
                    ["suggested_tags"] = new[] { "organic", "fresh", "natural", "quality" }
                };

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing product image: {ImageUrl}", imageUrl);
                return new Dictionary<string, object>();
            }
        }

        public async Task<List<string>> ExtractProductFeaturesAsync(string description)
        {
            try
            {
                // Simulate AI feature extraction
                var features = new List<string>();
                var words = description.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                var featureKeywords = new[] { "organic", "natural", "fresh", "quality", "premium", "certified", "sustainable", "eco-friendly" };
                
                foreach (var word in words)
                {
                    if (featureKeywords.Contains(word))
                    {
                        features.Add(word);
                    }
                }

                return features.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting product features from description");
                return new List<string>();
            }
        }

        public async Task<decimal> PredictProductDemandAsync(int productId)
        {
            try
            {
                // Simulate demand prediction based on historical data
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return 0;

                var historicalOrders = await _context.OrderItems
                    .Where(oi => oi.ProductId == productId)
                    .SumAsync(oi => oi.Quantity);

                var viewCount = product.ViewCount;
                var rating = product.Rating;

                // Simple demand prediction algorithm
                var demandScore = (historicalOrders * 0.4m) + (viewCount * 0.3m) + (rating * 0.3m);
                
                return Math.Max(0, demandScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting demand for product {ProductId}", productId);
                return 0;
            }
        }

        public async Task<List<string>> SuggestSearchKeywordsAsync(string category)
        {
            try
            {
                // Simulate keyword suggestions based on category
                var suggestions = new List<string>();
                
                switch (category.ToLower())
                {
                    case "fresh":
                        suggestions.AddRange(new[] { "organic", "local", "seasonal", "farm-fresh", "pesticide-free" });
                        break;
                    case "dried":
                        suggestions.AddRange(new[] { "preserved", "dehydrated", "long-shelf-life", "bulk", "wholesale" });
                        break;
                    case "fruit":
                        suggestions.AddRange(new[] { "fresh-fruit", "tropical", "seasonal-fruit", "organic-fruit" });
                        break;
                    case "herbs":
                        suggestions.AddRange(new[] { "medicinal", "culinary", "organic-herbs", "fresh-herbs" });
                        break;
                    default:
                        suggestions.AddRange(new[] { "quality", "premium", "organic", "natural", "certified" });
                        break;
                }

                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suggesting search keywords for category: {Category}", category);
                return new List<string>();
            }
        }

        private async Task<List<string>> GetUserPreferencesAsync(string userId)
        {
            try
            {
                var userOrders = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .ToListAsync();

                var preferences = new Dictionary<string, int>();
                
                foreach (var order in userOrders)
                {
                    foreach (var item in order.OrderItems)
                    {
                        var category = item.Product?.Category?.ToLower();
                        if (!string.IsNullOrEmpty(category))
                        {
                            preferences[category] = preferences.GetValueOrDefault(category, 0) + item.Quantity;
                        }
                    }
                }

                return preferences.OrderByDescending(x => x.Value).Take(5).Select(x => x.Key).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user preferences for user {UserId}", userId);
                return new List<string>();
            }
        }
    }
} 