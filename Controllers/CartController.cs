using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AgroProductEcommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroProductEcommerce.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var cartId = GetCartId();
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CartId == cartId)
                .ToListAsync();

            return View(cartItems);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            try
            {
                // Log the request
                System.Diagnostics.Debug.WriteLine($"AddToCart called: ProductId={productId}, Quantity={quantity}");
                System.Diagnostics.Debug.WriteLine($"Is AJAX: {Request.Headers["X-Requested-With"] == "XMLHttpRequest"}");
                
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    System.Diagnostics.Debug.WriteLine("Product not found");
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Product not found" });
                    }
                    return NotFound();
                }

                // Enhanced validation for B2B platform
                var validationResult = ValidateAddToCart(product, quantity);
                if (!validationResult.IsValid)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = validationResult.ErrorMessage });
                    }
                    TempData["ErrorMessage"] = validationResult.ErrorMessage;
                    return RedirectToAction("Index", "Products");
                }

                var cartId = GetCartId();
                System.Diagnostics.Debug.WriteLine($"CartId: {cartId}");
                
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.ProductId == productId && c.CartId == cartId);

                if (cartItem == null)
                {
                    cartItem = new CartItem
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        CartId = cartId,
                        AddedDate = DateTime.UtcNow,
                        UnitPrice = GetProductPrice(product, quantity), // Store the price at time of adding
                        Notes = GetCartItemNotes(product, quantity)
                    };
                    _context.CartItems.Add(cartItem);
                    System.Diagnostics.Debug.WriteLine("New cart item created");
                }
                else
                {
                    cartItem.Quantity += quantity;
                    cartItem.UnitPrice = GetProductPrice(product, cartItem.Quantity); // Update price for new total
                    cartItem.UpdatedDate = DateTime.UtcNow;
                    cartItem.Notes = GetCartItemNotes(product, cartItem.Quantity);
                    System.Diagnostics.Debug.WriteLine($"Updated existing cart item. New quantity: {cartItem.Quantity}");
                }

                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Database saved successfully");

                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var cartItemCount = await GetCartItemCount(cartId);
                    var cartTotal = await GetCartTotal(cartId);
                    System.Diagnostics.Debug.WriteLine($"Returning JSON response. Cart item count: {cartItemCount}");
                    return Json(new { 
                        success = true, 
                        message = GetSuccessMessage(product, quantity),
                        cartItemCount = cartItemCount,
                        cartTotal = cartTotal,
                        productName = product.Name,
                        unitPrice = cartItem.UnitPrice,
                        totalPrice = cartItem.UnitPrice * cartItem.Quantity
                    });
                }

                TempData["SuccessMessage"] = GetSuccessMessage(product, quantity);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in AddToCart: {ex.Message}");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "An error occurred while adding to cart" });
                }
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCart(int id, int quantity)
        {
            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id);
                
            if (cartItem == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Cart item not found" });
                }
                return NotFound();
            }

            // Validate quantity update
            var validationResult = ValidateUpdateCart(cartItem.Product, quantity);
            if (!validationResult.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = validationResult.ErrorMessage });
                }
                TempData["ErrorMessage"] = validationResult.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            cartItem.Quantity = quantity;
            cartItem.UnitPrice = GetProductPrice(cartItem.Product, quantity);
            cartItem.UpdatedDate = DateTime.UtcNow;
            cartItem.Notes = GetCartItemNotes(cartItem.Product, quantity);
            
            await _context.SaveChangesAsync();

            // Check if this is an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var cartId = GetCartId();
                var cartTotal = await GetCartTotal(cartId);
                return Json(new { 
                    success = true, 
                    message = "Cart updated successfully!",
                    cartItemCount = await GetCartItemCount(cartId),
                    cartTotal = cartTotal,
                    unitPrice = cartItem.UnitPrice,
                    totalPrice = cartItem.UnitPrice * cartItem.Quantity
                });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }

            // Check if this is an AJAX request
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var cartId = GetCartId();
                var cartTotal = await GetCartTotal(cartId);
                return Json(new { 
                    success = true, 
                    message = "Item removed from cart successfully!",
                    cartItemCount = await GetCartItemCount(cartId),
                    cartTotal = cartTotal
                });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var cartId = GetCartId();
            var cartItems = await _context.CartItems
                .Where(c => c.CartId == cartId)
                .ToListAsync();

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { 
                    success = true, 
                    message = "Cart cleared successfully!",
                    cartItemCount = 0,
                    cartTotal = 0
                });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> SaveForLater(int id)
        {
            var cartItem = await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Cart item not found" });
            }

            // Add to wishlist or saved items (implement based on your wishlist system)
            // For now, we'll just mark it as saved
            cartItem.IsSavedForLater = true;
            cartItem.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"{cartItem.Product.Name} saved for later!"
            });
        }

        private string GetCartId()
        {
            var cartId = HttpContext.Session.GetString("CartId");
            if (string.IsNullOrEmpty(cartId))
            {
                cartId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("CartId", cartId);
            }
            return cartId;
        }

        [Authorize]
        public async Task MergeCarts(ClaimsPrincipal userPrincipal)
        {
            // Get the current session cart ID
            var sessionCartId = HttpContext.Session.GetString("CartId");

            if (string.IsNullOrEmpty(sessionCartId))
            {
                return; // No session cart to merge
            }

            // Get the user ID from the provided principal
            var userId = _userManager.GetUserId(userPrincipal);

            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User ID cannot be null");
            }

            // If session cart is already the user's cart, nothing to do
            if (sessionCartId == userId)
            {
                return;
            }

            // Get items from session cart
            var sessionCartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CartId == sessionCartId)
                .ToListAsync();

            if (!sessionCartItems.Any())
            {
                return; // No items to merge
            }

            // Get user's existing cart items
            var userCartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CartId == userId)
                .ToListAsync();

            // Merge items
            foreach (var sessionItem in sessionCartItems)
            {
                var existingItem = userCartItems.FirstOrDefault(c => c.ProductId == sessionItem.ProductId);

                if (existingItem != null)
                {
                    // Update quantity if product already exists in user's cart
                    existingItem.Quantity += sessionItem.Quantity;
                    existingItem.UnitPrice = GetProductPrice(sessionItem.Product, existingItem.Quantity);
                    existingItem.UpdatedDate = DateTime.UtcNow;
                    existingItem.Notes = GetCartItemNotes(sessionItem.Product, existingItem.Quantity);
                    _context.CartItems.Remove(sessionItem);
                    _context.Update(existingItem);
                }
                else
                {
                    // Add to user's cart if product doesn't exist
                    sessionItem.CartId = userId;
                    sessionItem.UnitPrice = GetProductPrice(sessionItem.Product, sessionItem.Quantity);
                    sessionItem.Notes = GetCartItemNotes(sessionItem.Product, sessionItem.Quantity);
                    _context.Update(sessionItem);
                }
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("CartId");
        }

        private async Task<int> GetCartItemCount(string cartId)
        {
            return await _context.CartItems
                .Where(c => c.CartId == cartId)
                .SumAsync(c => c.Quantity);
        }

        private async Task<decimal> GetCartTotal(string cartId)
        {
            return await _context.CartItems
                .Where(c => c.CartId == cartId)
                .SumAsync(c => c.UnitPrice * c.Quantity);
        }

        // Enhanced validation for B2B platform
        private ValidationResult ValidateAddToCart(Product product, int quantity)
        {
            if (quantity <= 0)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Quantity must be greater than 0" };
            }

            if (product.StockQuantity < quantity)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"Only {product.StockQuantity} units available in stock" };
            }

            if (product.MinimumOrderQuantity.HasValue && quantity < product.MinimumOrderQuantity.Value)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"Minimum order quantity is {product.MinimumOrderQuantity.Value} units" };
            }

            if (product.MaximumOrderQuantity.HasValue && quantity > product.MaximumOrderQuantity.Value)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"Maximum order quantity is {product.MaximumOrderQuantity.Value} units" };
            }

            // Check if product is available for purchase
            if (product.Status != ProductStatus.Active)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "This product is not available for purchase" };
            }

            return new ValidationResult { IsValid = true };
        }

        private ValidationResult ValidateUpdateCart(Product product, int quantity)
        {
            if (quantity <= 0)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Quantity must be greater than 0" };
            }

            if (product.StockQuantity < quantity)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"Only {product.StockQuantity} units available in stock" };
            }

            if (product.MinimumOrderQuantity.HasValue && quantity < product.MinimumOrderQuantity.Value)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = $"Minimum order quantity is {product.MinimumOrderQuantity.Value} units" };
            }

            return new ValidationResult { IsValid = true };
        }

        // Get product price based on quantity (bulk pricing)
        private decimal GetProductPrice(Product product, int quantity)
        {
            if (product.BulkPrice.HasValue && product.BulkQuantity.HasValue && quantity >= product.BulkQuantity.Value)
            {
                return product.BulkPrice.Value;
            }
            return product.Price;
        }

        // Get cart item notes based on product and quantity
        private string GetCartItemNotes(Product product, int quantity)
        {
            var notes = new List<string>();

            if (product.BulkPrice.HasValue && product.BulkQuantity.HasValue && quantity >= product.BulkQuantity.Value)
            {
                notes.Add($"Bulk pricing applied ({product.BulkQuantity.Value}+ units)");
            }

            if (product.TradeAssuranceEligible)
            {
                notes.Add("Trade Assurance eligible");
            }

            if (!string.IsNullOrEmpty(product.QualityGrade))
            {
                notes.Add($"Quality: {product.QualityGrade}");
            }

            if (product.LeadTimeDays.HasValue)
            {
                notes.Add($"Lead time: {product.LeadTimeDays.Value} days");
            }

            return string.Join("; ", notes);
        }

        // Get success message based on product and quantity
        private string GetSuccessMessage(Product product, int quantity)
        {
            var messages = new List<string>();
            messages.Add($"{quantity} {product.Name} added to cart");

            if (product.BulkPrice.HasValue && product.BulkQuantity.HasValue && quantity >= product.BulkQuantity.Value)
            {
                messages.Add("Bulk pricing applied!");
            }

            if (product.TradeAssuranceEligible)
            {
                messages.Add("Trade Assurance protection included");
            }

            return string.Join(" ", messages);
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }
}