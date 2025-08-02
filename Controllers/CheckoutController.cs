using System.Linq;
using System.Threading.Tasks;
using AgroProductEcommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroProductEcommerce.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            var cartId = GetCartId();
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CartId == cartId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.CartTotal = cartItems.Sum(c => c.Product.Price * c.Quantity);

            var order = new Order
            {
                CustomerName = $"{user.FirstName} {user.LastName}",
                Email = user.Email,
                BillingAddress = user.Address ?? "",
                BillingCity = user.City ?? "",
                BillingState = user.State ?? "",
                BillingPostalCode = user.PostalCode ?? "",
                BillingCountry = user.Country ?? "",
                ShippingAddress = user.Address ?? "",
                ShippingCity = user.City ?? "",
                ShippingState = user.State ?? "",
                ShippingPostalCode = user.PostalCode ?? "",
                ShippingCountry = user.Country ?? "",
                PhoneNumber = user.PhoneNumber,
                UserId = userId
            };

            return View(order);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(Order order)
        {
            var userId = _userManager.GetUserId(User);

            if (!ModelState.IsValid)
            {
                var cartId = GetCartId();
                var cartItems = await _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.CartId == cartId)
                    .ToListAsync();

                ViewBag.CartTotal = cartItems.Sum(c => c.Product.Price * c.Quantity);
                return View("Index", order);
            }

            var cartIdForOrder = GetCartId();
            var cartItemsForOrder = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.CartId == cartIdForOrder)
                .ToListAsync();

            if (!cartItemsForOrder.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            order.TotalAmount = cartItemsForOrder.Sum(c => c.Product.Price * c.Quantity);
            order.OrderDate = DateTime.Now;
            order.UserId = userId;

            foreach (var item in cartItemsForOrder)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price,
                    TotalPrice = item.Product.Price * item.Quantity,
                    ProductName = item.Product.Name,
                    ProductDescription = item.Product.Description,
                    ProductImageUrl = item.Product.ImageUrl
                });
            }

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItemsForOrder);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("CartId");

            return RedirectToAction("OrderConfirmation", new { id = order.Id });
        }
        //public async Task<IActionResult> Index()
        //{
        //    var cartId = GetCartId();
        //    var cartItems = await _context.CartItems
        //        .Include(c => c.Product)
        //        .Where(c => c.CartId == cartId)
        //        .ToListAsync();

        //    if (!cartItems.Any())
        //    {
        //        return RedirectToAction("Index", "Cart");
        //    }

        //    ViewBag.CartTotal = cartItems.Sum(c => c.Product.Price * c.Quantity);

        //    return View(new Order());
        //}

        //[HttpPost]
        //public async Task<IActionResult> PlaceOrder(Order order)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View("Index", order);
        //    }

        //    var cartId = GetCartId();
        //    var cartItems = await _context.CartItems
        //        .Include(c => c.Product)
        //        .Where(c => c.CartId == cartId)
        //        .ToListAsync();

        //    if (!cartItems.Any())
        //    {
        //        return RedirectToAction("Index", "Cart");
        //    }

        //    order.TotalAmount = cartItems.Sum(c => c.Product.Price * c.Quantity);
        //    order.OrderDate = DateTime.Now;

        //    foreach (var item in cartItems)
        //    {
        //        order.OrderItems.Add(new OrderItem
        //        {
        //            ProductId = item.ProductId,
        //            Quantity = item.Quantity,
        //            Price = item.Product.Price
        //        });
        //    }

        //    _context.Orders.Add(order);
        //    _context.CartItems.RemoveRange(cartItems);
        //    await _context.SaveChangesAsync();

        //    HttpContext.Session.Remove("CartId");

        //    return RedirectToAction("OrderConfirmation", new { id = order.Id });
        //}

        public IActionResult OrderConfirmation(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
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
    }
}