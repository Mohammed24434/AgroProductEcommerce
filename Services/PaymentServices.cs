using System.Text.Json;
using AgroProductEcommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace AgroProductEcommerce.Services
{
    public enum PaymentGateway
    {
        Stripe,
        PayPal,
        BankTransfer,
        Escrow,
        TradeAssurance,
        LetterOfCredit
    }

    public enum EscrowStatus
    {
        Pending,
        Funded,
        Released,
        Refunded,
        Disputed
    }

    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(Order order, PaymentMethod paymentMethod, string currency = "USD");
        Task<EscrowResult> CreateEscrowAsync(Order order, decimal amount);
        Task<EscrowResult> ReleaseEscrowAsync(string escrowId, string reason);
        Task<EscrowResult> RefundEscrowAsync(string escrowId, string reason);
        Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency);
        Task<List<string>> GetSupportedCurrenciesAsync();
        Task<PaymentValidationResult> ValidatePaymentAsync(Order order, PaymentMethod paymentMethod);
        Task<TradeAssuranceResult> CreateTradeAssuranceAsync(Order order);
        Task<PaymentResult> ProcessBulkPaymentAsync(List<Order> orders, PaymentMethod paymentMethod);
    }

    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public PaymentService(ApplicationDbContext context, ILogger<PaymentService> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(Order order, PaymentMethod paymentMethod, string currency = "USD")
        {
            try
            {
                var result = new PaymentResult
                {
                    Success = false,
                    OrderId = order.Id,
                    PaymentMethod = paymentMethod,
                    Currency = currency
                };

                // Validate payment
                var validation = await ValidatePaymentAsync(order, paymentMethod);
                if (!validation.IsValid)
                {
                    result.ErrorMessage = validation.ErrorMessage;
                    return result;
                }

                // Get exchange rate if needed
                if (currency != order.Currency)
                {
                    var exchangeRate = await GetExchangeRateAsync(order.Currency, currency);
                    order.ExchangeRate = exchangeRate;
                    order.TotalAmount = order.TotalAmount * exchangeRate;
                }

                switch (paymentMethod)
                {
                    case PaymentMethod.CreditCard:
                        result = await ProcessCreditCardPaymentAsync(order, currency);
                        break;
                    case PaymentMethod.PayPal:
                        result = await ProcessPayPalPaymentAsync(order, currency);
                        break;
                    case PaymentMethod.BankTransfer:
                        result = await ProcessBankTransferAsync(order, currency);
                        break;
                    case PaymentMethod.Escrow:
                        result = await ProcessEscrowPaymentAsync(order, currency);
                        break;
                    case PaymentMethod.TradeAssurance:
                        result = await ProcessTradeAssurancePaymentAsync(order, currency);
                        break;
                    case PaymentMethod.LetterOfCredit:
                        result = await ProcessLetterOfCreditAsync(order, currency);
                        break;
                    case PaymentMethod.CashOnDelivery:
                        result = await ProcessCashOnDeliveryAsync(order, currency);
                        break;
                    default:
                        result.ErrorMessage = "Unsupported payment method";
                        break;
                }

                if (result.Success)
                {
                    // Update order status
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.PaymentDate = DateTime.UtcNow;
                    order.PaymentGateway = result.PaymentGateway.ToString();
                    order.TransactionId = result.TransactionId;

                    await _context.SaveChangesAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for order {OrderId}", order.Id);
                return new PaymentResult
                {
                    Success = false,
                    OrderId = order.Id,
                    ErrorMessage = "Payment processing failed"
                };
            }
        }

        public async Task<EscrowResult> CreateEscrowAsync(Order order, decimal amount)
        {
            try
            {
                var escrow = new Models.EscrowTransaction
                {
                    Id = 0, // Will be auto-generated by EF
                    OrderId = order.Id,
                    BuyerId = order.UserId,
                    SupplierId = order.OrderItems.FirstOrDefault()?.SupplierId,
                    Amount = amount,
                    Currency = order.Currency,
                    Status = EscrowStatus.Pending.ToString(),
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                _context.EscrowTransactions.Add(escrow);
                await _context.SaveChangesAsync();

                return new EscrowResult
                {
                    Success = true,
                    EscrowId = escrow.Id.ToString(),
                    Status = EscrowStatus.Pending.ToString(),
                    Amount = amount,
                    Currency = order.Currency
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating escrow for order {OrderId}", order.Id);
                return new EscrowResult
                {
                    Success = false,
                    ErrorMessage = "Failed to create escrow"
                };
            }
        }

        public async Task<EscrowResult> ReleaseEscrowAsync(string escrowId, string reason)
        {
            try
            {
                if (!int.TryParse(escrowId, out int id))
                {
                    return new EscrowResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid escrow ID format"
                    };
                }
                
                var escrow = await _context.EscrowTransactions.FindAsync(id);
                if (escrow == null)
                {
                    return new EscrowResult
                    {
                        Success = false,
                        ErrorMessage = "Escrow not found"
                    };
                }

                escrow.Status = EscrowStatus.Released.ToString();
                escrow.ReleasedDate = DateTime.UtcNow;
                escrow.Notes = $"Released: {reason}";

                await _context.SaveChangesAsync();

                return new EscrowResult
                {
                    Success = true,
                    EscrowId = escrowId,
                    Status = EscrowStatus.Released.ToString(),
                    Amount = escrow.Amount,
                    Currency = escrow.Currency
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing escrow {EscrowId}", escrowId);
                return new EscrowResult
                {
                    Success = false,
                    ErrorMessage = "Failed to release escrow"
                };
            }
        }

        public async Task<EscrowResult> RefundEscrowAsync(string escrowId, string reason)
        {
            try
            {
                if (!int.TryParse(escrowId, out int id))
                {
                    return new EscrowResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid escrow ID format"
                    };
                }
                
                var escrow = await _context.EscrowTransactions.FindAsync(id);
                if (escrow == null)
                {
                    return new EscrowResult
                    {
                        Success = false,
                        ErrorMessage = "Escrow not found"
                    };
                }

                escrow.Status = EscrowStatus.Refunded.ToString();
                escrow.RefundedDate = DateTime.UtcNow;
                escrow.Notes = $"Refunded: {reason}";

                await _context.SaveChangesAsync();

                return new EscrowResult
                {
                    Success = true,
                    EscrowId = escrowId,
                    Status = EscrowStatus.Refunded.ToString(),
                    Amount = escrow.Amount,
                    Currency = escrow.Currency
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding escrow {EscrowId}", escrowId);
                return new EscrowResult
                {
                    Success = false,
                    ErrorMessage = "Failed to refund escrow"
                };
            }
        }

        public async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency)
        {
            try
            {
                // Simulate exchange rate API call
                var rates = new Dictionary<string, decimal>
                {
                    ["USD"] = 1.0m,
                    ["EUR"] = 0.85m,
                    ["GBP"] = 0.73m,
                    ["JPY"] = 110.0m,
                    ["CNY"] = 6.45m,
                    ["INR"] = 74.0m,
                    ["AED"] = 3.67m,
                    ["SAR"] = 3.75m
                };

                if (rates.ContainsKey(fromCurrency) && rates.ContainsKey(toCurrency))
                {
                    return rates[toCurrency] / rates[fromCurrency];
                }

                return 1.0m; // Default to 1:1 if currency not found
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exchange rate from {FromCurrency} to {ToCurrency}", fromCurrency, toCurrency);
                return 1.0m;
            }
        }

        public async Task<List<string>> GetSupportedCurrenciesAsync()
        {
            return new List<string>
            {
                "USD", "EUR", "GBP", "JPY", "CNY", "INR", "AED", "SAR", "CAD", "AUD"
            };
        }

        public async Task<PaymentValidationResult> ValidatePaymentAsync(Order order, PaymentMethod paymentMethod)
        {
            var result = new PaymentValidationResult { IsValid = true };

            // Validate order amount
            if (order.TotalAmount <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid order amount";
                return result;
            }

            // Validate user
            if (string.IsNullOrEmpty(order.UserId))
            {
                result.IsValid = false;
                result.ErrorMessage = "User not found";
                return result;
            }

            // Validate payment method for order type
            if (order.IsB2B && paymentMethod == PaymentMethod.CreditCard)
            {
                result.IsValid = false;
                result.ErrorMessage = "Credit card not supported for B2B orders";
                return result;
            }

            // Validate trade assurance eligibility
            if (paymentMethod == PaymentMethod.TradeAssurance)
            {
                var user = await _context.Users.FindAsync(order.UserId);
                if (user == null || !user.TradeAssuranceEnabled)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Trade Assurance not enabled for this user";
                    return result;
                }
            }

            return result;
        }

        public async Task<TradeAssuranceResult> CreateTradeAssuranceAsync(Order order)
        {
            try
            {
                var user = await _context.Users.FindAsync(order.UserId);
                if (user == null || !user.TradeAssuranceEnabled)
                {
                    return new TradeAssuranceResult
                    {
                        Success = false,
                        ErrorMessage = "Trade Assurance not available"
                    };
                }

                var assuranceAmount = Math.Min(order.TotalAmount, user.TradeAssuranceLimit);

                var assurance = new Models.TradeAssurance
                {
                    Id = 0, // Will be auto-generated by EF
                    OrderId = order.Id,
                    BuyerId = order.UserId,
                    SupplierId = order.OrderItems.FirstOrDefault()?.SupplierId,
                    CoverageAmount = assuranceAmount,
                    Currency = order.Currency,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddDays(90)
                };

                _context.TradeAssurances.Add(assurance);
                await _context.SaveChangesAsync();

                return new TradeAssuranceResult
                {
                    Success = true,
                    AssuranceId = assurance.Id.ToString(),
                    Amount = assuranceAmount,
                    Currency = order.Currency
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trade assurance for order {OrderId}", order.Id);
                return new TradeAssuranceResult
                {
                    Success = false,
                    ErrorMessage = "Failed to create trade assurance"
                };
            }
        }

        public async Task<PaymentResult> ProcessBulkPaymentAsync(List<Order> orders, PaymentMethod paymentMethod)
        {
            try
            {
                var results = new List<PaymentResult>();
                var totalAmount = orders.Sum(o => o.TotalAmount);

                foreach (var order in orders)
                {
                    var result = await ProcessPaymentAsync(order, paymentMethod);
                    results.Add(result);
                }

                var successCount = results.Count(r => r.Success);
                var failedCount = results.Count(r => !r.Success);

                return new PaymentResult
                {
                    Success = failedCount == 0,
                    OrderId = orders.First().Id,
                    PaymentMethod = paymentMethod,
                    TransactionId = Guid.NewGuid().ToString("N"),
                    Amount = totalAmount,
                    Currency = orders.First().Currency,
                    ErrorMessage = failedCount > 0 ? $"{failedCount} payments failed" : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk payment for {OrderCount} orders", orders.Count);
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = "Bulk payment processing failed"
                };
            }
        }

        private async Task<PaymentResult> ProcessCreditCardPaymentAsync(Order order, string currency)
        {
            // Simulate credit card payment processing
            await Task.Delay(1000); // Simulate API call

            return new PaymentResult
            {
                Success = true,
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.CreditCard,
                PaymentGateway = PaymentGateway.Stripe,
                TransactionId = Guid.NewGuid().ToString("N"),
                Amount = order.TotalAmount,
                Currency = currency
            };
        }

        private async Task<PaymentResult> ProcessPayPalPaymentAsync(Order order, string currency)
        {
            // Simulate PayPal payment processing
            await Task.Delay(1500); // Simulate API call

            return new PaymentResult
            {
                Success = true,
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.PayPal,
                PaymentGateway = PaymentGateway.PayPal,
                TransactionId = Guid.NewGuid().ToString("N"),
                Amount = order.TotalAmount,
                Currency = currency
            };
        }

        private async Task<PaymentResult> ProcessBankTransferAsync(Order order, string currency)
        {
            // Simulate bank transfer processing
            await Task.Delay(2000); // Simulate API call

            return new PaymentResult
            {
                Success = true,
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.BankTransfer,
                PaymentGateway = PaymentGateway.BankTransfer,
                TransactionId = Guid.NewGuid().ToString("N"),
                Amount = order.TotalAmount,
                Currency = currency
            };
        }

        private async Task<PaymentResult> ProcessEscrowPaymentAsync(Order order, string currency)
        {
            var escrowResult = await CreateEscrowAsync(order, order.TotalAmount);
            
            return new PaymentResult
            {
                Success = escrowResult.Success,
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.Escrow,
                PaymentGateway = PaymentGateway.Escrow,
                TransactionId = escrowResult.EscrowId,
                Amount = order.TotalAmount,
                Currency = currency,
                ErrorMessage = escrowResult.ErrorMessage
            };
        }

        private async Task<PaymentResult> ProcessTradeAssurancePaymentAsync(Order order, string currency)
        {
            var assuranceResult = await CreateTradeAssuranceAsync(order);
            
            return new PaymentResult
            {
                Success = assuranceResult.Success,
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.TradeAssurance,
                PaymentGateway = PaymentGateway.TradeAssurance,
                TransactionId = assuranceResult.AssuranceId,
                Amount = order.TotalAmount,
                Currency = currency,
                ErrorMessage = assuranceResult.ErrorMessage
            };
        }

        private async Task<PaymentResult> ProcessLetterOfCreditAsync(Order order, string currency)
        {
            // Simulate letter of credit processing
            await Task.Delay(3000); // Simulate API call

            return new PaymentResult
            {
                Success = true,
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.LetterOfCredit,
                PaymentGateway = PaymentGateway.BankTransfer,
                TransactionId = Guid.NewGuid().ToString("N"),
                Amount = order.TotalAmount,
                Currency = currency
            };
        }

        private async Task<PaymentResult> ProcessCashOnDeliveryAsync(Order order, string currency)
        {
            // Simulate cash on delivery processing
            await Task.Delay(500); // Simulate API call

            return new PaymentResult
            {
                Success = true,
                OrderId = order.Id,
                PaymentMethod = PaymentMethod.CashOnDelivery,
                PaymentGateway = PaymentGateway.BankTransfer,
                TransactionId = Guid.NewGuid().ToString("N"),
                Amount = order.TotalAmount,
                Currency = currency
            };
        }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public int OrderId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentGateway PaymentGateway { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class EscrowResult
    {
        public bool Success { get; set; }
        public string EscrowId { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class TradeAssuranceResult
    {
        public bool Success { get; set; }
        public string AssuranceId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PaymentValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }


} 