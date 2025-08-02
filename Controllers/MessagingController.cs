using AgroProductEcommerce.Models;
using AgroProductEcommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace AgroProductEcommerce.Controllers
{
    [Authorize]
    public class MessagingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<MessagingController> _logger;

        public MessagingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<MessagingController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = _userManager.GetUserId(User);

                // Get conversations (unique users the current user has messaged with)
                var conversations = await _context.Messages
                    .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                    .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                    .Distinct()
                    .ToListAsync();

                var conversationUsers = await _context.Users
                    .Where(u => conversations.Contains(u.Id))
                    .ToListAsync();

                // Get latest message for each conversation
                var conversationList = new List<ConversationViewModel>();
                foreach (var user in conversationUsers)
                {
                    var latestMessage = await _context.Messages
                        .Where(m => (m.SenderId == userId && m.ReceiverId == user.Id) ||
                                   (m.SenderId == user.Id && m.ReceiverId == userId))
                        .OrderByDescending(m => m.CreatedDate)
                        .FirstOrDefaultAsync();

                    var unreadCount = await _context.Messages
                        .Where(m => m.SenderId == user.Id && m.ReceiverId == userId && !m.IsRead)
                        .CountAsync();

                    conversationList.Add(new ConversationViewModel
                    {
                        User = user,
                        LatestMessage = latestMessage,
                        UnreadCount = unreadCount
                    });
                }

                // Sort by latest message date
                conversationList = conversationList
                    .OrderByDescending(c => c.LatestMessage?.CreatedDate)
                    .ToList();

                return View(conversationList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversations for user {UserId}", _userManager.GetUserId(User));
                return View("Error");
            }
        }

        public async Task<IActionResult> Conversation(string userId)
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                var otherUser = await _userManager.FindByIdAsync(userId);

                if (otherUser == null)
                {
                    return NotFound();
                }

                // Get messages between the two users
                var messages = await _context.Messages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                               (m.SenderId == userId && m.ReceiverId == currentUserId))
                    .OrderBy(m => m.CreatedDate)
                    .ToListAsync();

                // Mark messages as read
                var unreadMessages = messages.Where(m => m.SenderId == userId && !m.IsRead).ToList();
                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                    message.ReadDate = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();

                var conversationViewModel = new ConversationDetailViewModel
                {
                    OtherUser = otherUser,
                    Messages = messages
                };

                return View(conversationViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading conversation with user {UserId}", userId);
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(string receiverId, string subject, string content, MessageType type = MessageType.General)
        {
            try
            {
                var senderId = _userManager.GetUserId(User);
                var receiver = await _userManager.FindByIdAsync(receiverId);

                if (receiver == null)
                {
                    return Json(new { success = false, message = "Receiver not found" });
                }

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Subject = subject,
                    Content = EncryptMessage(content),
                    Type = type,
                    IsEncrypted = true,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Message sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to user {ReceiverId}", receiverId);
                return Json(new { success = false, message = "An error occurred while sending the message" });
            }
        }

        public async Task<IActionResult> Negotiations()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var negotiations = await _context.Messages
                    .Where(m => m.Type == MessageType.Negotiation &&
                               (m.SenderId == userId || m.ReceiverId == userId))
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .Include(m => m.Product)
                    .Include(m => m.Order)
                    .OrderByDescending(m => m.CreatedDate)
                    .ToListAsync();

                return View(negotiations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading negotiations for user {UserId}", _userManager.GetUserId(User));
                return View("Error");
            }
        }

        public async Task<IActionResult> DisputeMessages(int disputeId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var dispute = await _context.Disputes
                    .Include(d => d.Initiator)
                    .Include(d => d.Respondent)
                    .Include(d => d.Order)
                    .Include(d => d.Product)
                    .FirstOrDefaultAsync(d => d.Id == disputeId);

                if (dispute == null)
                {
                    return NotFound();
                }

                // Check if user is involved in the dispute
                if (dispute.InitiatorId != userId && dispute.RespondentId != userId)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }

                var messages = await _context.Messages
                    .Where(m => m.DisputeId == disputeId)
                    .Include(m => m.Sender)
                    .OrderBy(m => m.CreatedDate)
                    .ToListAsync();

                var disputeViewModel = new DisputeMessageViewModel
                {
                    Dispute = dispute,
                    Messages = messages
                };

                return View(disputeViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dispute messages for dispute {DisputeId}", disputeId);
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendDisputeMessage(int disputeId, string content)
        {
            try
            {
                var senderId = _userManager.GetUserId(User);
                var dispute = await _context.Disputes.FindAsync(disputeId);

                if (dispute == null)
                {
                    return Json(new { success = false, message = "Dispute not found" });
                }

                // Check if user is involved in the dispute
                if (dispute.InitiatorId != senderId && dispute.RespondentId != senderId)
                {
                    return Json(new { success = false, message = "Access denied" });
                }

                var receiverId = dispute.InitiatorId == senderId ? dispute.RespondentId : dispute.InitiatorId;

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    DisputeId = disputeId,
                    Subject = $"Dispute #{disputeId} - {dispute.Title}",
                    Content = EncryptMessage(content),
                    Type = MessageType.Dispute,
                    IsEncrypted = true,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Message sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending dispute message for dispute {DisputeId}", disputeId);
                return Json(new { success = false, message = "An error occurred while sending the message" });
            }
        }

        public async Task<IActionResult> ProductInquiry(int productId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var product = await _context.Products
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                {
                    return NotFound();
                }

                var inquiryViewModel = new ProductInquiryViewModel
                {
                    Product = product,
                    Message = new Message
                    {
                        ProductId = productId,
                        ReceiverId = product.SupplierId,
                        Type = MessageType.General
                    }
                };

                return View(inquiryViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product inquiry for product {ProductId}", productId);
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendProductInquiry(int productId, string subject, string content)
        {
            try
            {
                var senderId = _userManager.GetUserId(User);
                var product = await _context.Products
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = product.SupplierId,
                    ProductId = productId,
                    Subject = subject,
                    Content = EncryptMessage(content),
                    Type = MessageType.General,
                    IsEncrypted = true,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Inquiry sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending product inquiry for product {ProductId}", productId);
                return Json(new { success = false, message = "An error occurred while sending the inquiry" });
            }
        }

        public async Task<IActionResult> OrderInquiry(int orderId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Supplier)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

                if (order == null)
                {
                    return NotFound();
                }

                var inquiryViewModel = new OrderInquiryViewModel
                {
                    Order = order,
                    Message = new Message
                    {
                        OrderId = orderId,
                        Type = MessageType.General
                    }
                };

                return View(inquiryViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order inquiry for order {OrderId}", orderId);
                return View("Error");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendOrderInquiry(int orderId, string receiverId, string subject, string content)
        {
            try
            {
                var senderId = _userManager.GetUserId(User);
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == senderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    OrderId = orderId,
                    Subject = subject,
                    Content = EncryptMessage(content),
                    Type = MessageType.General,
                    IsEncrypted = true,
                    CreatedDate = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Inquiry sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order inquiry for order {OrderId}", orderId);
                return Json(new { success = false, message = "An error occurred while sending the inquiry" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var unreadCount = await _context.Messages
                    .Where(m => m.ReceiverId == userId && !m.IsRead)
                    .CountAsync();

                return Json(new { count = unreadCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", _userManager.GetUserId(User));
                return Json(new { count = 0 });
            }
        }

        private string EncryptMessage(string message)
        {
            // Simple encryption for demonstration - in production, use proper encryption
            var key = "AgroProductEcommerce2024";
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];

                using (var encryptor = aes.CreateEncryptor())
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (var swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(message);
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        private string DecryptMessage(string encryptedMessage)
        {
            try
            {
                var key = "AgroProductEcommerce2024";
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                    aes.IV = new byte[16];

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedMessage)))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch
            {
                return "Message could not be decrypted";
            }
        }
    }

    public class ConversationViewModel
    {
        public ApplicationUser User { get; set; }
        public Message LatestMessage { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ConversationDetailViewModel
    {
        public ApplicationUser OtherUser { get; set; }
        public List<Message> Messages { get; set; }
    }

    public class DisputeMessageViewModel
    {
        public Dispute Dispute { get; set; }
        public List<Message> Messages { get; set; }
    }

    public class ProductInquiryViewModel
    {
        public Product Product { get; set; }
        public Message Message { get; set; }
    }

    public class OrderInquiryViewModel
    {
        public Order Order { get; set; }
        public Message Message { get; set; }
    }
} 