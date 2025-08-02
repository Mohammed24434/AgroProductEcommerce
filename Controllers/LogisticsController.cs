using AgroProductEcommerce.Models;
using AgroProductEcommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroProductEcommerce.Controllers
{
    [Authorize]
    public class LogisticsController : Controller
    {
        private readonly ILogisticsService _logisticsService;
        private readonly ILogger<LogisticsController> _logger;

        public LogisticsController(
            ILogisticsService logisticsService,
            ILogger<LogisticsController> logger)
        {
            _logisticsService = logisticsService;
            _logger = logger;
        }

        public IActionResult ShippingCalculator()
        {
            return View(new Models.LogisticsRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculateShipping(Models.LogisticsRequest request)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var quote = await _logisticsService.CalculateShippingCostAsync(request);
                    
                    if (quote.TotalCost > 0)
                    {
                        return Json(new { success = true, quote = quote });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Unable to calculate shipping cost" });
                    }
                }

                return Json(new { success = false, message = "Invalid request data" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shipping cost");
                return Json(new { success = false, message = "An error occurred while calculating shipping cost" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetMultipleQuotes(Models.LogisticsRequest request)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var quotes = await _logisticsService.GetMultipleQuotesAsync(request);
                    return Json(new { success = true, quotes = quotes });
                }

                return Json(new { success = false, message = "Invalid request data" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting multiple shipping quotes");
                return Json(new { success = false, message = "An error occurred while getting shipping quotes" });
            }
        }

        public async Task<IActionResult> TrackShipment(string trackingNumber, string carrier)
        {
            try
            {
                if (string.IsNullOrEmpty(trackingNumber) || string.IsNullOrEmpty(carrier))
                {
                    return View(new LogisticsTracking
                    {
                        ErrorMessage = "Tracking number and carrier are required"
                    });
                }

                var tracking = await _logisticsService.TrackShipmentAsync(trackingNumber, carrier);
                return View(tracking);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking shipment {TrackingNumber}", trackingNumber);
                return View(new LogisticsTracking
                {
                    ErrorMessage = "An error occurred while tracking the shipment"
                });
            }
        }

        public async Task<IActionResult> ShippingMethods(string fromCountry, string toCountry)
        {
            try
            {
                var shippingMethods = await _logisticsService.GetAvailableShippingMethodsAsync(fromCountry, toCountry);
                return Json(new { success = true, methods = shippingMethods });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipping methods from {FromCountry} to {ToCountry}", fromCountry, toCountry);
                return Json(new { success = false, message = "An error occurred while getting shipping methods" });
            }
        }

        public async Task<IActionResult> CustomsInfo(string fromCountry, string toCountry, string hsCode)
        {
            try
            {
                var customsInfo = await _logisticsService.GetCustomsInformationAsync(fromCountry, toCountry, hsCode);
                return Json(new { success = true, customsInfo = customsInfo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customs information for {HSCode} from {FromCountry} to {ToCountry}", hsCode, fromCountry, toCountry);
                return Json(new { success = false, message = "An error occurred while getting customs information" });
            }
        }

        public async Task<IActionResult> DeliveryEstimate(Models.LogisticsRequest request)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var estimate = await _logisticsService.GetDeliveryEstimateAsync(request);
                    return Json(new { success = true, estimate = estimate });
                }

                return Json(new { success = false, message = "Invalid request data" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delivery estimate");
                return Json(new { success = false, message = "An error occurred while getting delivery estimate" });
            }
        }

        public async Task<IActionResult> AvailableCarriers(string fromCountry, string toCountry)
        {
            try
            {
                var carriers = await _logisticsService.GetAvailableCarriersAsync(fromCountry, toCountry);
                return Json(new { success = true, carriers = carriers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available carriers from {FromCountry} to {ToCountry}", fromCountry, toCountry);
                return Json(new { success = false, message = "An error occurred while getting available carriers" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkShippingQuote(List<Models.LogisticsRequest> requests)
        {
            try
            {
                if (requests == null || !requests.Any())
                {
                    return Json(new { success = false, message = "No requests provided" });
                }

                var quote = await _logisticsService.GetBulkShippingQuoteAsync(requests);
                return Json(new { success = true, quote = quote });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bulk shipping quote");
                return Json(new { success = false, message = "An error occurred while getting bulk shipping quote" });
            }
        }

        public IActionResult ShippingGuide()
        {
            var guideViewModel = new ShippingGuideViewModel
            {
                Countries = GetCountries(),
                HS_Codes = GetHSCodes(),
                ShippingTips = GetShippingTips()
            };

            return View(guideViewModel);
        }

        public IActionResult CustomsGuide()
        {
            var customsGuide = new CustomsGuideViewModel
            {
                DocumentationRequirements = GetDocumentationRequirements(),
                RestrictedItems = GetRestrictedItems(),
                DutyRates = GetDutyRates()
            };

            return View(customsGuide);
        }

        private List<string> GetCountries()
        {
            return new List<string>
            {
                "US", "CA", "MX", "GB", "DE", "FR", "IT", "ES", "CN", "JP", "KR", "IN", "AU", "NZ", "BR", "AR", "ZA", "EG", "NG"
            };
        }

        private List<string> GetHSCodes()
        {
            return new List<string>
            {
                "0701.90", "0702.00", "0703.10", "0703.20", "0704.10", "0704.20", "0705.11", "0705.19", "0706.10", "0706.90",
                "0707.00", "0708.10", "0708.20", "0709.10", "0709.20", "0709.30", "0709.40", "0709.50", "0709.60", "0709.70",
                "0709.90", "0710.10", "0710.20", "0710.30", "0710.40", "0710.50", "0710.60", "0710.70", "0710.80", "0710.90"
            };
        }

        private List<string> GetShippingTips()
        {
            return new List<string>
            {
                "Always declare the correct value of your goods",
                "Use appropriate packaging for fragile items",
                "Include detailed product descriptions",
                "Ensure all required documentation is complete",
                "Check import restrictions before shipping",
                "Consider insurance for valuable items",
                "Use tracking services for important shipments",
                "Plan for customs clearance time"
            };
        }

        private List<string> GetDocumentationRequirements()
        {
            return new List<string>
            {
                "Commercial Invoice",
                "Packing List",
                "Certificate of Origin",
                "Bill of Lading",
                "Customs Declaration",
                "Import License (if required)",
                "Phytosanitary Certificate (for agricultural products)",
                "Fumigation Certificate (if required)"
            };
        }

        private List<string> GetRestrictedItems()
        {
            return new List<string>
            {
                "Live animals",
                "Perishable goods without proper packaging",
                "Hazardous materials",
                "Counterfeit goods",
                "Illegal substances",
                "Weapons and ammunition",
                "Radioactive materials",
                "Endangered species products"
            };
        }

        private Dictionary<string, decimal> GetDutyRates()
        {
            return new Dictionary<string, decimal>
            {
                ["0701.90"] = 0.05m, // Potatoes
                ["0702.00"] = 0.05m, // Tomatoes
                ["0703.10"] = 0.05m, // Onions
                ["0704.10"] = 0.05m, // Cabbage
                ["0705.11"] = 0.05m, // Lettuce
                ["0706.10"] = 0.05m, // Carrots
                ["0707.00"] = 0.05m, // Cucumbers
                ["0708.10"] = 0.05m, // Peas
                ["0709.10"] = 0.05m, // Artichokes
                ["0709.20"] = 0.05m  // Asparagus
            };
        }
    }

    public class ShippingGuideViewModel
    {
        public List<string> Countries { get; set; }
        public List<string> HS_Codes { get; set; }
        public List<string> ShippingTips { get; set; }
    }

    public class CustomsGuideViewModel
    {
        public List<string> DocumentationRequirements { get; set; }
        public List<string> RestrictedItems { get; set; }
        public Dictionary<string, decimal> DutyRates { get; set; }
    }
} 