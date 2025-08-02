using System.Linq;
using System.Threading.Tasks;
using AgroProductEcommerce.Models;
using AgroProductEcommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroProductEcommerce.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAIService _aiService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAIService aiService,
            ILogger<AdminController> logger)
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
                // Get platform statistics
                var totalUsers = await _context.Users.CountAsync();
                var totalProducts = await _context.Products.CountAsync();
                var totalOrders = await _context.Orders.CountAsync();
                var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);

                // Get pending KYC verifications
                var pendingKYC = await _context.Users
                    .Where(u => u.KYCStatus == KYCStatus.Pending)
                    .CountAsync();

                // Get active disputes
                var activeDisputes = await _context.Disputes
                    .Where(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview)
                    .CountAsync();

                // Get recent activities
                var recentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.CreatedDate)
                    .Take(10)
                    .ToListAsync();

                var recentUsers = await _context.Users
                    .OrderByDescending(u => u.RegistrationDate)
                    .Take(10)
                    .ToListAsync();

                var dashboardViewModel = new AdminDashboardViewModel
                {
                    TotalUsers = totalUsers,
                    TotalProducts = totalProducts,
                    TotalOrders = totalOrders,
                    TotalRevenue = totalRevenue,
                    PendingKYC = pendingKYC,
                    ActiveDisputes = activeDisputes,
                    RecentOrders = recentOrders,
                    RecentUsers = recentUsers
                };

                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                return View("Error");
            }
        }

        public async Task<IActionResult> KYCManagement()
        {
            try
            {
                var pendingKYC = await _context.Users
                    .Where(u => u.KYCStatus == KYCStatus.Pending)
                    .OrderBy(u => u.RegistrationDate)
                    .ToListAsync();

                var underReviewKYC = await _context.Users
                    .Where(u => u.KYCStatus == KYCStatus.UnderReview)
                    .OrderBy(u => u.RegistrationDate)
                    .ToListAsync();

                var kycViewModel = new KYCManagementViewModel
                {
                    PendingUsers = pendingKYC,
                    UnderReviewUsers = underReviewKYC
                };

                return View(kycViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading KYC management");
                return View("Error");
            }
        }

        public async Task<IActionResult> KYCDetails(string userId)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound();
                }

                var supplierProfile = await _context.SupplierProfiles
                    .FirstOrDefaultAsync(sp => sp.UserId == userId);

                var buyerProfile = await _context.BuyerProfiles
                    .FirstOrDefaultAsync(bp => bp.UserId == userId);

                var kycDetailsViewModel = new KYCDetailsViewModel
                {
                    User = user,
                    SupplierProfile = supplierProfile,
                    BuyerProfile = buyerProfile
                };

                return View(kycDetailsViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading KYC details for user {UserId}", userId);
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveKYC(string userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.KYCStatus = KYCStatus.Verified;
                user.KYCVerifiedDate = DateTime.UtcNow;
                user.KYCVerifiedBy = _userManager.GetUserId(User);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "KYC approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving KYC for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while approving KYC" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectKYC(string userId, string reason)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.KYCStatus = KYCStatus.Rejected;
                user.KYCRejectionReason = reason;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "KYC rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting KYC for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while rejecting KYC" });
            }
        }

        public async Task<IActionResult> DisputeManagement()
        {
            try
            {
                var disputes = await _context.Disputes
                    .Include(d => d.Initiator)
                    .Include(d => d.Respondent)
                    .Include(d => d.Order)
                    .Include(d => d.Product)
                    .OrderByDescending(d => d.CreatedDate)
                    .ToListAsync();

                return View(disputes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dispute management");
                return View("Error");
            }
        }

        public async Task<IActionResult> DisputeDetails(int id)
        {
            try
            {
                var dispute = await _context.Disputes
                    .Include(d => d.Initiator)
                    .Include(d => d.Respondent)
                    .Include(d => d.Order)
                    .Include(d => d.Product)
                    .Include(d => d.Messages)
                    .ThenInclude(m => m.Sender)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (dispute == null)
                {
                    return NotFound();
                }

                return View(dispute);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dispute details for dispute {DisputeId}", id);
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResolveDispute(int disputeId, string resolution, decimal? refundAmount)
        {
            try
            {
                var dispute = await _context.Disputes.FindAsync(disputeId);
                if (dispute == null)
                {
                    return Json(new { success = false, message = "Dispute not found" });
                }

                dispute.Status = DisputeStatus.Resolved;
                dispute.Resolution = resolution;
                dispute.ResolvedBy = _userManager.GetUserId(User);
                dispute.ResolvedDate = DateTime.UtcNow;

                if (refundAmount.HasValue)
                {
                    dispute.RefundAmount = refundAmount.Value;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Dispute resolved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving dispute {DisputeId}", disputeId);
                return Json(new { success = false, message = "An error occurred while resolving the dispute" });
            }
        }

        public async Task<IActionResult> Products()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Supplier)
                    .Include(p => p.Reviews)
                    .OrderByDescending(p => p.CreatedDate)
                    .ToListAsync();

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products for admin");
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApproveProduct(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                product.Status = ProductStatus.Active;
                product.PublishedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Product approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving product {ProductId}", productId);
                return Json(new { success = false, message = "An error occurred while approving the product" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectProduct(int productId, string reason)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                product.Status = ProductStatus.Inactive;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Product rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting product {ProductId}", productId);
                return Json(new { success = false, message = "An error occurred while rejecting the product" });
            }
        }

        public async Task<IActionResult> Analytics()
        {
            try
            {
                // Get sales analytics
                var salesData = await _context.Orders
                    .Where(o => o.CreatedDate >= DateTime.UtcNow.AddMonths(-6))
                    .GroupBy(o => new { Month = o.CreatedDate.Month, Year = o.CreatedDate.Year })
                    .Select(g => new { Month = g.Key.Month, Year = g.Key.Year, Revenue = g.Sum(o => o.TotalAmount), Count = g.Count() })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                // Get user growth
                var userGrowth = await _context.Users
                    .Where(u => u.RegistrationDate >= DateTime.UtcNow.AddMonths(-6))
                    .GroupBy(u => new { Month = u.RegistrationDate.Month, Year = u.RegistrationDate.Year })
                    .Select(g => new { Month = g.Key.Month, Year = g.Key.Year, Count = g.Count() })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                // Get top products
                var topProducts = await _context.OrderItems
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(oi => oi.Quantity) })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(10)
                    .ToListAsync();

                // Get category performance
                var categoryPerformance = await _context.Products
                    .GroupBy(p => p.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                var analyticsViewModel = new AdminAnalyticsViewModel
                {
                    SalesData = salesData.Cast<object>().ToList(),
                    UserGrowth = userGrowth.Cast<object>().ToList(),
                    TopProducts = topProducts.Cast<object>().ToList(),
                    CategoryPerformance = categoryPerformance.Cast<object>().ToList()
                };

                return View(analyticsViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics for admin");
                return View("Error");
            }
        }

        public async Task<IActionResult> UserManagement()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.SupplierProfiles)
                    .Include(u => u.BuyerProfiles)
                    .OrderByDescending(u => u.RegistrationDate)
                    .ToListAsync();

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user management");
                return View("Error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(string userId, string role)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!string.IsNullOrEmpty(role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }

                return Json(new { success = true, message = "User role updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while updating the user role" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.LockoutEnd = DateTimeOffset.MaxValue;
                await _userManager.UpdateAsync(user);

                return Json(new { success = true, message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while deactivating the user" });
            }
        }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingKYC { get; set; }
        public int ActiveDisputes { get; set; }
        public List<Order> RecentOrders { get; set; }
        public List<ApplicationUser> RecentUsers { get; set; }
    }

    public class KYCManagementViewModel
    {
        public List<ApplicationUser> PendingUsers { get; set; }
        public List<ApplicationUser> UnderReviewUsers { get; set; }
    }

    public class KYCDetailsViewModel
    {
        public ApplicationUser User { get; set; }
        public SupplierProfile SupplierProfile { get; set; }
        public BuyerProfile BuyerProfile { get; set; }
    }

    public class AdminAnalyticsViewModel
    {
        public List<object> SalesData { get; set; }
        public List<object> UserGrowth { get; set; }
        public List<object> TopProducts { get; set; }
        public List<object> CategoryPerformance { get; set; }
    }
}