using AgroProductEcommerce.Models;
using AgroProductEcommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroProductEcommerce.Controllers
{
    [Authorize(Roles = "Buyer")]
    public class BuyerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAIService _aiService;
        private readonly ILogisticsService _logisticsService;
        private readonly ILogger<BuyerController> _logger;

        public BuyerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAIService aiService,
            ILogisticsService logisticsService,
            ILogger<BuyerController> logger)
        {
            _context = context;
            _userManager = userManager;
            _aiService = aiService;
            _logisticsService = logisticsService;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                // Get buyer profile
                var buyerProfile = await _context.BuyerProfiles
                    .FirstOrDefaultAsync(bp => bp.UserId == userId);

                if (buyerProfile == null)
                {
                    return RedirectToAction("CompleteProfile");
                }

                // Get recent orders
                var recentOrders = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .ToListAsync();

                // Get active RFQs
                var activeRFQs = await _context.RFQs
                    .Where(r => r.BuyerId == userId && r.Status == RFQStatus.Published)
                    .Include(r => r.Responses)
                    .OrderByDescending(r => r.CreatedDate)
                    .Take(5)
                    .ToListAsync();

                // Get personalized recommendations
                var recommendations = await _aiService.GetPersonalizedRecommendationsAsync(userId, 6);

                var totalSpent = recentOrders.Sum(o => o.TotalAmount);
                var totalOrders = recentOrders.Count;
                var activeRFQCount = activeRFQs.Count;

                var dashboardViewModel = new BuyerDashboardViewModel
                {
                    BuyerProfile = buyerProfile,
                    RecentOrders = recentOrders,
                    ActiveRFQs = activeRFQs,
                    Recommendations = recommendations,
                    TotalSpent = totalSpent,
                    TotalOrders = totalOrders,
                    ActiveRFQCount = activeRFQCount
                };

                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading buyer dashboard for user {UserId}", _userManager.GetUserId(User));
                return View("Error");
            }
        }

        public async Task<IActionResult> CreateRFQ()
        {
            var categories = await GetCategoriesAsync();
            ViewBag.Categories = categories;
            return View(new RFQ());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRFQ(RFQ rfq)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userId = _userManager.GetUserId(User);
                    rfq.BuyerId = userId;
                    rfq.Status = RFQStatus.Draft;
                    rfq.CreatedDate = DateTime.UtcNow;
                    rfq.UpdatedDate = DateTime.UtcNow;

                    _context.RFQs.Add(rfq);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "RFQ created successfully!";
                    return RedirectToAction(nameof(MyRFQs));
                }

                var categories = await GetCategoriesAsync();
                ViewBag.Categories = categories;
                return View(rfq);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating RFQ for buyer {UserId}", _userManager.GetUserId(User));
                ModelState.AddModelError("", "An error occurred while creating the RFQ.");
                var categories = await GetCategoriesAsync();
                ViewBag.Categories = categories;
                return View(rfq);
            }
        }

        public async Task<IActionResult> MyRFQs()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var rfqs = await _context.RFQs
                    .Where(r => r.BuyerId == userId)
                    .Include(r => r.Responses)
                    .ThenInclude(rr => rr.Supplier)
                    .OrderByDescending(r => r.CreatedDate)
                    .ToListAsync();

                return View(rfqs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading RFQs for buyer {UserId}", _userManager.GetUserId(User));
                return View("Error");
            }
        }

        public async Task<IActionResult> RFQDetails(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var rfq = await _context.RFQs
                    .Where(r => r.Id == id && r.BuyerId == userId)
                    .Include(r => r.RFQItems)
                    .Include(r => r.Responses)
                    .ThenInclude(rr => rr.Supplier)
                    .FirstOrDefaultAsync();

                if (rfq == null)
                {
                    return NotFound();
                }

                return View(rfq);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading RFQ details for RFQ {RFQId}", id);
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> PublishRFQ(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var rfq = await _context.RFQs
                    .FirstOrDefaultAsync(r => r.Id == id && r.BuyerId == userId);

                if (rfq == null)
                {
                    return Json(new { success = false, message = "RFQ not found" });
                }

                rfq.Status = RFQStatus.Published;
                rfq.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "RFQ published successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing RFQ {RFQId}", id);
                return Json(new { success = false, message = "An error occurred while publishing the RFQ" });
            }
        }

        public async Task<IActionResult> BrowseRFQs()
        {
            try
            {
                var publishedRFQs = await _context.RFQs
                    .Where(r => r.Status == RFQStatus.Published)
                    .Include(r => r.Buyer)
                    .Include(r => r.Responses)
                    .OrderByDescending(r => r.CreatedDate)
                    .ToListAsync();

                return View(publishedRFQs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading published RFQs");
                return View("Error");
            }
        }

        public async Task<IActionResult> RespondToRFQ(int rfqId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                // Check if user is a supplier
                if (user.UserType != UserType.Supplier)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }

                var rfq = await _context.RFQs
                    .Include(r => r.RFQItems)
                    .FirstOrDefaultAsync(r => r.Id == rfqId && r.Status == RFQStatus.Published);

                if (rfq == null)
                {
                    return NotFound();
                }

                var response = new RFQResponse
                {
                    RFQId = rfqId,
                    SupplierId = userId,
                    Quantity = rfq.Quantity,
                    Unit = rfq.Unit
                };

                ViewBag.RFQ = rfq;
                return View(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading RFQ response form for RFQ {RFQId}", rfqId);
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToRFQ(int rfqId, RFQResponse response)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                if (user.UserType != UserType.Supplier)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }

                if (ModelState.IsValid)
                {
                    response.RFQId = rfqId;
                    response.SupplierId = userId;
                    response.CreatedDate = DateTime.UtcNow;
                    response.UpdatedDate = DateTime.UtcNow;

                    _context.RFQResponses.Add(response);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Response submitted successfully!";
                    return RedirectToAction("BrowseRFQs");
                }

                var rfq = await _context.RFQs
                    .Include(r => r.RFQItems)
                    .FirstOrDefaultAsync(r => r.Id == rfqId);
                ViewBag.RFQ = rfq;
                return View(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting RFQ response for RFQ {RFQId}", rfqId);
                ModelState.AddModelError("", "An error occurred while submitting the response.");
                var rfq = await _context.RFQs
                    .Include(r => r.RFQItems)
                    .FirstOrDefaultAsync(r => r.Id == rfqId);
                ViewBag.RFQ = rfq;
                return View(response);
            }
        }

        public async Task<IActionResult> BulkOrder()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var products = await _context.Products
                    .Where(p => p.Status == ProductStatus.Active && p.StockQuantity > 0)
                    .Include(p => p.Supplier)
                    .ToListAsync();

                var bulkOrderViewModel = new BulkOrderViewModel
                {
                    Products = products,
                    OrderItems = new List<BulkOrderItem>()
                };

                return View(bulkOrderViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bulk order form");
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBulkOrder(BulkOrderViewModel model)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);

                if (ModelState.IsValid)
                {
                    var order = new Order
                    {
                        UserId = userId,
                        CustomerName = $"{user.FirstName} {user.LastName}",
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        BillingAddress = user.Address,
                        BillingCity = user.City,
                        BillingState = user.State,
                        BillingPostalCode = user.PostalCode,
                        BillingCountry = user.Country,
                        ShippingAddress = user.Address,
                        ShippingCity = user.City,
                        ShippingState = user.State,
                        ShippingPostalCode = user.PostalCode,
                        ShippingCountry = user.Country,
                        IsB2B = true,
                        Status = OrderStatus.Pending,
                        PaymentStatus = PaymentStatus.Pending,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };

                    var orderItems = new List<OrderItem>();
                    decimal totalAmount = 0;

                    foreach (var item in model.OrderItems.Where(oi => oi.Quantity > 0))
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            var unitPrice = item.Quantity >= (product.BulkQuantity ?? 1) 
                                ? (product.BulkPrice ?? product.Price) 
                                : product.Price;

                            var orderItem = new OrderItem
                            {
                                ProductId = item.ProductId,
                                Quantity = item.Quantity,
                                UnitPrice = unitPrice,
                                TotalPrice = unitPrice * item.Quantity,
                                SupplierId = product.SupplierId,
                                ProductName = product.Name,
                                ProductDescription = product.Description,
                                ProductImageUrl = product.ImageUrl,
                                Currency = "USD",
                                CreatedDate = DateTime.UtcNow,
                                UpdatedDate = DateTime.UtcNow
                            };

                            orderItems.Add(orderItem);
                            totalAmount += orderItem.TotalPrice;
                        }
                    }

                    order.OrderItems = orderItems;
                    order.Subtotal = totalAmount;
                    order.TotalAmount = totalAmount;
                    order.ItemCount = orderItems.Count;

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Bulk order created successfully!";
                    return RedirectToAction("OrderConfirmation", "Checkout", new { orderId = order.Id });
                }

                var products = await _context.Products
                    .Where(p => p.Status == ProductStatus.Active && p.StockQuantity > 0)
                    .Include(p => p.Supplier)
                    .ToListAsync();
                model.Products = products;
                return View("BulkOrder", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk order for buyer {UserId}", _userManager.GetUserId(User));
                ModelState.AddModelError("", "An error occurred while creating the bulk order.");
                var products = await _context.Products
                    .Where(p => p.Status == ProductStatus.Active && p.StockQuantity > 0)
                    .Include(p => p.Supplier)
                    .ToListAsync();
                model.Products = products;
                return View("BulkOrder", model);
            }
        }

        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var user = await _userManager.GetUserAsync(User);
                var buyerProfile = await _context.BuyerProfiles
                    .FirstOrDefaultAsync(bp => bp.UserId == userId);

                var profileViewModel = new BuyerProfileViewModel
                {
                    User = user,
                    BuyerProfile = buyerProfile
                };

                return View(profileViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for buyer {UserId}", _userManager.GetUserId(User));
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(BuyerProfileViewModel model)
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

                    // Update or create buyer profile
                    var buyerProfile = await _context.BuyerProfiles
                        .FirstOrDefaultAsync(bp => bp.UserId == userId);

                    if (buyerProfile == null)
                    {
                        buyerProfile = new BuyerProfile
                        {
                            UserId = userId,
                            CompanyName = model.User.CompanyName ?? user.CompanyName
                        };
                        _context.BuyerProfiles.Add(buyerProfile);
                    }

                    if (model.BuyerProfile != null)
                    {
                        buyerProfile.CompanyName = model.BuyerProfile.CompanyName;
                        buyerProfile.Industry = model.BuyerProfile.Industry;
                        buyerProfile.BusinessType = model.BuyerProfile.BusinessType;
                        buyerProfile.EmployeeCount = model.BuyerProfile.EmployeeCount;
                        buyerProfile.AnnualPurchasingBudget = model.BuyerProfile.AnnualPurchasingBudget;
                        buyerProfile.PreferredShippingMethod = model.BuyerProfile.PreferredShippingMethod;
                        buyerProfile.MinimumOrderValue = model.BuyerProfile.MinimumOrderValue;
                        buyerProfile.UpdatedDate = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Profile updated successfully!";
                    return RedirectToAction(nameof(Profile));
                }

                return View("Profile", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for buyer {UserId}", _userManager.GetUserId(User));
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
    }

    public class BuyerDashboardViewModel
    {
        public BuyerProfile BuyerProfile { get; set; }
        public List<Order> RecentOrders { get; set; }
        public List<RFQ> ActiveRFQs { get; set; }
        public List<Product> Recommendations { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalOrders { get; set; }
        public int ActiveRFQCount { get; set; }
    }

    public class BuyerProfileViewModel
    {
        public ApplicationUser User { get; set; }
        public BuyerProfile BuyerProfile { get; set; }
    }

    public class BulkOrderViewModel
    {
        public List<Product> Products { get; set; }
        public List<BulkOrderItem> OrderItems { get; set; }
    }

    public class BulkOrderItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string SupplierName { get; set; }
        public decimal Price { get; set; }
        public decimal? BulkPrice { get; set; }
        public int? BulkQuantity { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
    }
} 