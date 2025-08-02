using AgroProductEcommerce.Models;
using AgroProductEcommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroProductEcommerce.Controllers
{
    [Authorize(Roles = "Supplier")]
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAIService _aiService;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAIService aiService,
            ILogger<SupplierController> logger)
        {
            _context = context;
            _userManager = userManager;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                // Get supplier profile
                var supplierProfile = await _context.SupplierProfiles
                    .FirstOrDefaultAsync(sp => sp.UserId == userId);

                if (supplierProfile == null)
                {
                    return RedirectToAction("CompleteProfile");
                }

                // Get recent orders
                var recentOrders = await _context.Orders
                    .Where(o => o.OrderItems.Any(oi => oi.SupplierId == userId))
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync();

                // Get product analytics
                var products = await _context.Products
                    .Where(p => p.SupplierId == userId)
                    .ToListAsync();

                var totalRevenue = recentOrders.Sum(o => o.TotalAmount);
                var totalProducts = products.Count;
                var activeOrders = recentOrders.Count(o => o.Status == OrderStatus.Processing || o.Status == OrderStatus.Shipped);

                var dashboardViewModel = new SupplierDashboardViewModel
                {
                    SupplierProfile = supplierProfile,
                    RecentOrders = recentOrders,
                    Products = products,
                    TotalRevenue = totalRevenue,
                    TotalProducts = totalProducts,
                    ActiveOrders = activeOrders,
                    TotalOrders = recentOrders.Count
                };

                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading supplier dashboard for user {UserId}", _userManager.GetUserId(User));
                return View("Error");
            }
        }

        public async Task<IActionResult> Products()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var products = await _context.Products
                    .Where(p => p.SupplierId == userId)
                    .Include(p => p.ProductImages)
                    .Include(p => p.Reviews)
                    .OrderByDescending(p => p.CreatedDate)
                    .ToListAsync();

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products for supplier {UserId}", _userManager.GetUserId(User));
                return View("Error");
            }
        }

        public async Task<IActionResult> CreateProduct()
        {
            var categories = await GetCategoriesAsync();
            ViewBag.Categories = categories;
            return View(new Product());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product product, IFormFile? imageFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userId = _userManager.GetUserId(User);
                    product.SupplierId = userId;
                    product.Status = ProductStatus.Active;
                    product.CreatedDate = DateTime.UtcNow;
                    product.UpdatedDate = DateTime.UtcNow;

                    // Generate AI tags
                    var aiTags = await _aiService.GenerateProductTagsAsync(product);
                    product.AI_Tags = string.Join(",", aiTags);

                    // Handle image upload
                    if (imageFile != null)
                    {
                        var imageUrl = await SaveProductImageAsync(imageFile);
                        product.ImageUrl = imageUrl;
                    }

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Product created successfully!";
                    return RedirectToAction(nameof(Products));
                }

                var categories = await GetCategoriesAsync();
                ViewBag.Categories = categories;
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product for supplier {UserId}", _userManager.GetUserId(User));
                ModelState.AddModelError("", "An error occurred while creating the product.");
                var categories = await GetCategoriesAsync();
                ViewBag.Categories = categories;
                return View(product);
            }
        }

        public async Task<IActionResult> EditProduct(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var product = await _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.Id == id && p.SupplierId == userId);

                if (product == null)
                {
                    return NotFound();
                }

                var categories = await GetCategoriesAsync();
                ViewBag.Categories = categories;
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product {ProductId} for editing", id);
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, Product product, IFormFile? imageFile)
        {
            try
            {
                if (id != product.Id)
                {
                    return NotFound();
                }

                var userId = _userManager.GetUserId(User);
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id && p.SupplierId == userId);

                if (existingProduct == null)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    // Update product properties
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.BulkPrice = product.BulkPrice;
                    existingProduct.BulkQuantity = product.BulkQuantity;
                    existingProduct.Category = product.Category;
                    existingProduct.SubCategory = product.SubCategory;
                    existingProduct.StockQuantity = product.StockQuantity;
                    existingProduct.MinimumOrderQuantity = product.MinimumOrderQuantity;
                    existingProduct.MaximumOrderQuantity = product.MaximumOrderQuantity;
                    existingProduct.LeadTimeDays = product.LeadTimeDays;
                    existingProduct.UpdatedDate = DateTime.UtcNow;

                    // Handle image upload
                    if (imageFile != null)
                    {
                        var imageUrl = await SaveProductImageAsync(imageFile);
                        existingProduct.ImageUrl = imageUrl;
                    }

                    // Regenerate AI tags
                    var aiTags = await _aiService.GenerateProductTagsAsync(existingProduct);
                    existingProduct.AI_Tags = string.Join(",", aiTags);

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Products));
                }

                var categories = await GetCategoriesAsync();
                ViewBag.Categories = categories;
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                ModelState.AddModelError("", "An error occurred while updating the product.");
                var categories = await GetCategoriesAsync();
                ViewBag.Categories = categories;
                return View(product);
            }
        }

        public async Task<IActionResult> Orders()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var orders = await _context.Orders
                    .Where(o => o.OrderItems.Any(oi => oi.SupplierId == userId))
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders for supplier {UserId}", _userManager.GetUserId(User));
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == orderId && 
                                            o.OrderItems.Any(oi => oi.SupplierId == userId));

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                order.Status = status;
                order.UpdatedDate = DateTime.UtcNow;

                if (status == OrderStatus.Shipped)
                {
                    order.ShippedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Order status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", orderId);
                return Json(new { success = false, message = "An error occurred while updating the order status" });
            }
        }

        public async Task<IActionResult> Analytics()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var startDate = DateTime.UtcNow.AddMonths(-6);

                // Get sales data
                var salesData = await _context.Orders
                    .Where(o => o.OrderItems.Any(oi => oi.SupplierId == userId) &&
                               o.OrderDate >= startDate)
                    .GroupBy(o => new { Month = o.OrderDate.Month, Year = o.OrderDate.Year })
                    .Select(g => new { Month = g.Key.Month, Year = g.Key.Year, Revenue = g.Sum(o => o.TotalAmount) })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                // Get top products
                var topProducts = await _context.OrderItems
                    .Where(oi => oi.SupplierId == userId)
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(oi => oi.Quantity) })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(10)
                    .ToListAsync();

                // Get demand predictions
                var products = await _context.Products
                    .Where(p => p.SupplierId == userId)
                    .ToListAsync();

                var demandPredictions = new List<DemandPrediction>();
                foreach (var product in products)
                {
                    var predictedDemand = await _aiService.PredictProductDemandAsync(product.Id);
                    demandPredictions.Add(new DemandPrediction
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        PredictedDemand = predictedDemand
                    });
                }

                var analyticsViewModel = new SupplierAnalyticsViewModel
                {
                    SalesData = salesData.Cast<object>().ToList(),
                    TopProducts = topProducts.Cast<object>().ToList(),
                    DemandPredictions = demandPredictions
                };

                return View(analyticsViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics for supplier {UserId}", _userManager.GetUserId(User));
                return View("Error");
            }
        }

        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);
                var supplierProfile = await _context.SupplierProfiles
                    .FirstOrDefaultAsync(sp => sp.UserId == userId);

                var profileViewModel = new SupplierProfileViewModel
                {
                    User = user,
                    SupplierProfile = supplierProfile
                };

                return View(profileViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for supplier {UserId}", _userManager.GetUserId(User));
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(SupplierProfileViewModel model)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                if (ModelState.IsValid)
                {
                    // Update user information
                    user.FirstName = model.User.FirstName;
                    user.LastName = model.User.LastName;
                    user.CompanyName = model.User.CompanyName;
                    user.Address = model.User.Address;
                    user.City = model.User.City;
                    user.State = model.User.State;
                    user.PostalCode = model.User.PostalCode;
                    user.Country = model.User.Country;
                    user.PhoneNumber = model.User.PhoneNumber;

                    await _userManager.UpdateAsync(user);

                    // Update or create supplier profile
                    var supplierProfile = await _context.SupplierProfiles
                        .FirstOrDefaultAsync(sp => sp.UserId == userId);

                    if (supplierProfile == null)
                    {
                        supplierProfile = new SupplierProfile
                        {
                            UserId = userId,
                            CompanyName = model.User.CompanyName ?? user.CompanyName
                        };
                        _context.SupplierProfiles.Add(supplierProfile);
                    }

                    if (model.SupplierProfile != null)
                    {
                        supplierProfile.CompanyName = model.SupplierProfile.CompanyName;
                        supplierProfile.BusinessLicense = model.SupplierProfile.BusinessLicense;
                        supplierProfile.ISO_Certification = model.SupplierProfile.ISO_Certification;
                        supplierProfile.QualityCertification = model.SupplierProfile.QualityCertification;
                        supplierProfile.EmployeeCount = model.SupplierProfile.EmployeeCount;
                        supplierProfile.AnnualRevenue = model.SupplierProfile.AnnualRevenue;
                        supplierProfile.ManufacturingCapacity = model.SupplierProfile.ManufacturingCapacity;
                        supplierProfile.ProductionLeadTime = model.SupplierProfile.ProductionLeadTime;
                        supplierProfile.QualityControlProcess = model.SupplierProfile.QualityControlProcess;
                        supplierProfile.ContactPerson = model.SupplierProfile.ContactPerson;
                        supplierProfile.ContactPhone = model.SupplierProfile.ContactPhone;
                        supplierProfile.ContactEmail = model.SupplierProfile.ContactEmail;
                        supplierProfile.Website = model.SupplierProfile.Website;
                        supplierProfile.UpdatedDate = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Profile updated successfully!";
                    return RedirectToAction(nameof(Profile));
                }

                return View("Profile", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for supplier {UserId}", _userManager.GetUserId(User));
                ModelState.AddModelError("", "An error occurred while updating the profile.");
                return View("Profile", model);
            }
        }

        private async Task<List<string>> GetCategoriesAsync()
        {
            return new List<string>
            {
                "Fresh", "Dried", "Fruit", "Herbs", "Grains", "Nuts", "Spices", "Tea", "Coffee", "Other"
            };
        }

        private async Task<string> SaveProductImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return string.Empty;

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            var filePath = Path.Combine("wwwroot", "images", "products", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return "/images/products/" + fileName;
        }
    }

    public class SupplierDashboardViewModel
    {
        public SupplierProfile SupplierProfile { get; set; }
        public List<Order> RecentOrders { get; set; }
        public List<Product> Products { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveOrders { get; set; }
        public int TotalOrders { get; set; }
    }

    public class SupplierAnalyticsViewModel
    {
        public List<object> SalesData { get; set; }
        public List<object> TopProducts { get; set; }
        public List<DemandPrediction> DemandPredictions { get; set; }
    }

    public class SupplierProfileViewModel
    {
        public ApplicationUser User { get; set; }
        public SupplierProfile SupplierProfile { get; set; }
    }

    public class DemandPrediction
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal PredictedDemand { get; set; }
    }
} 