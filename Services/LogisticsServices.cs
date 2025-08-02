using AgroProductEcommerce.Models;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AgroProductEcommerce.Services
{
    public interface ILogisticsService
    {
        Task<LogisticsQuote> CalculateShippingCostAsync(Models.LogisticsRequest request);
        Task<List<LogisticsQuote>> GetMultipleQuotesAsync(Models.LogisticsRequest request);
        Task<LogisticsTracking> TrackShipmentAsync(string trackingNumber, string carrier);
        Task<List<ShippingMethod>> GetAvailableShippingMethodsAsync(string fromCountry, string toCountry);
        Task<CustomsInfo> GetCustomsInformationAsync(string fromCountry, string toCountry, string hsCode);
        Task<DeliveryEstimate> GetDeliveryEstimateAsync(Models.LogisticsRequest request);
        Task<List<Carrier>> GetAvailableCarriersAsync(string fromCountry, string toCountry);
        Task<LogisticsQuote> GetBulkShippingQuoteAsync(List<Models.LogisticsRequest> requests);
    }

    public class LogisticsService : ILogisticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LogisticsService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public LogisticsService(ApplicationDbContext context, ILogger<LogisticsService> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<LogisticsQuote> CalculateShippingCostAsync(Models.LogisticsRequest request)
        {
            try
            {
                var quote = new LogisticsQuote
                {
                    RequestId = Guid.NewGuid().ToString("N"),
                    FromCountry = request.OriginCountry,
                    FromCity = request.OriginCity,
                    ToCountry = request.DestinationCountry,
                    ToCity = request.DestinationCity,
                    Weight = request.Weight,
                    WeightUnit = "kg",
                    Dimensions = new Dimensions
                    {
                        Length = request.Length ?? 0,
                        Width = request.Width ?? 0,
                        Height = request.Height ?? 0
                    },
                    ShippingMethod = ParseLogisticsType(request.ShippingMethod),
                    Currency = "USD"
                };

                // Calculate base cost based on distance and weight
                var baseCost = await CalculateBaseCostAsync(request);
                quote.BaseCost = baseCost;

                // Add fuel surcharge
                var fuelSurcharge = baseCost * 0.15m; // 15% fuel surcharge
                quote.FuelSurcharge = fuelSurcharge;

                // Add customs and handling fees
                var customsFees = await CalculateCustomsFeesAsync(request);
                quote.CustomsFees = customsFees;

                // Add insurance cost
                var insuranceCost = baseCost * 0.02m; // 2% insurance
                quote.InsuranceCost = insuranceCost;

                // Calculate total cost
                quote.TotalCost = baseCost + fuelSurcharge + customsFees + insuranceCost;

                // Calculate delivery time
                var deliveryTime = await CalculateDeliveryTimeAsync(request);
                quote.EstimatedDays = deliveryTime;

                // Get carrier information
                quote.Carrier = await GetCarrierForRouteAsync(request);
                quote.ServiceLevel = GetServiceLevel(ParseLogisticsType(request.ShippingMethod));

                quote.CreatedDate = DateTime.UtcNow;
                quote.ExpiryDate = DateTime.UtcNow.AddHours(24);

                return quote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shipping cost for request from {FromCountry} to {ToCountry}", 
                    request.OriginCountry, request.DestinationCountry);
                return new LogisticsQuote
                {
                    ErrorMessage = "Failed to calculate shipping cost"
                };
            }
        }

        public async Task<List<LogisticsQuote>> GetMultipleQuotesAsync(Models.LogisticsRequest request)
        {
            try
            {
                var quotes = new List<LogisticsQuote>();
                var shippingMethods = await GetAvailableShippingMethodsAsync(request.OriginCountry, request.DestinationCountry);

                foreach (var method in shippingMethods)
                {
                    var modifiedRequest = new Models.LogisticsRequest
                    {
                        OriginCountry = request.OriginCountry,
                        OriginCity = request.OriginCity,
                        DestinationCountry = request.DestinationCountry,
                        DestinationCity = request.DestinationCity,
                        Weight = request.Weight,
                        ShippingMethod = method.Type.ToString(),
                        DeclaredValue = request.DeclaredValue ?? 0
                    };

                    var quote = await CalculateShippingCostAsync(modifiedRequest);
                    if (quote.TotalCost > 0)
                    {
                        quotes.Add(quote);
                    }
                }

                return quotes.OrderBy(q => q.TotalCost).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting multiple quotes for request from {FromCountry} to {ToCountry}", 
                    request.OriginCountry, request.DestinationCountry);
                return new List<LogisticsQuote>();
            }
        }

        public async Task<LogisticsTracking> TrackShipmentAsync(string trackingNumber, string carrier)
        {
            try
            {
                // Simulate tracking API call
                await Task.Delay(1000);

                var tracking = new LogisticsTracking
                {
                    TrackingNumber = trackingNumber,
                    Carrier = carrier,
                    Status = "In Transit",
                    CurrentLocation = "Distribution Center",
                    EstimatedDelivery = DateTime.UtcNow.AddDays(3),
                    LastUpdate = DateTime.UtcNow
                };

                // Simulate tracking events
                tracking.Events = new List<TrackingEvent>
                {
                    new TrackingEvent
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-2),
                        Location = "Origin Warehouse",
                        Status = "Package Picked Up",
                        Description = "Package has been picked up by carrier"
                    },
                    new TrackingEvent
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-1),
                        Location = "Transit Hub",
                        Status = "In Transit",
                        Description = "Package is in transit to destination"
                    },
                    new TrackingEvent
                    {
                        Timestamp = DateTime.UtcNow,
                        Location = "Distribution Center",
                        Status = "Out for Delivery",
                        Description = "Package is out for delivery"
                    }
                };

                return tracking;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking shipment {TrackingNumber} with carrier {Carrier}", 
                    trackingNumber, carrier);
                return new LogisticsTracking
                {
                    ErrorMessage = "Failed to track shipment"
                };
            }
        }

        public async Task<List<ShippingMethod>> GetAvailableShippingMethodsAsync(string fromCountry, string toCountry)
        {
            try
            {
                var methods = new List<ShippingMethod>();

                // Air shipping (always available)
                methods.Add(new ShippingMethod
                {
                    Type = LogisticsType.Air,
                    Name = "Express Air",
                    Description = "Fastest delivery option",
                    EstimatedDays = 3,
                    MaxWeight = 1000,
                    MaxDimensions = new Dimensions { Length = 150, Width = 150, Height = 150 }
                });

                methods.Add(new ShippingMethod
                {
                    Type = LogisticsType.Air,
                    Name = "Standard Air",
                    Description = "Standard air freight",
                    EstimatedDays = 5,
                    MaxWeight = 2000,
                    MaxDimensions = new Dimensions { Length = 200, Width = 200, Height = 200 }
                });

                // Sea shipping (for international)
                if (fromCountry != toCountry)
                {
                    methods.Add(new ShippingMethod
                    {
                        Type = LogisticsType.Sea,
                        Name = "Ocean Freight",
                        Description = "Cost-effective sea shipping",
                        EstimatedDays = 21,
                        MaxWeight = 50000,
                        MaxDimensions = new Dimensions { Length = 1200, Width = 240, Height = 240 }
                    });
                }

                // Land shipping (for same continent)
                if (IsSameContinent(fromCountry, toCountry))
                {
                    methods.Add(new ShippingMethod
                    {
                        Type = LogisticsType.Land,
                        Name = "Ground Shipping",
                        Description = "Reliable ground transportation",
                        EstimatedDays = 7,
                        MaxWeight = 5000,
                        MaxDimensions = new Dimensions { Length = 300, Width = 200, Height = 200 }
                    });
                }

                return methods;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available shipping methods from {FromCountry} to {ToCountry}", 
                    fromCountry, toCountry);
                return new List<ShippingMethod>();
            }
        }

        public async Task<CustomsInfo> GetCustomsInformationAsync(string fromCountry, string toCountry, string hsCode)
        {
            try
            {
                // Simulate customs information lookup
                await Task.Delay(500);

                var customsInfo = new CustomsInfo
                {
                    FromCountry = fromCountry,
                    ToCountry = toCountry,
                    HSCode = hsCode,
                    DutyRate = 0.05m, // 5% duty rate
                    TaxRate = 0.10m, // 10% tax rate
                    RequiresLicense = false,
                    RestrictedItems = false,
                    DocumentationRequired = new List<string>
                    {
                        "Commercial Invoice",
                        "Packing List",
                        "Certificate of Origin"
                    }
                };

                return customsInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customs information for {HSCode} from {FromCountry} to {ToCountry}", 
                    hsCode, fromCountry, toCountry);
                return new CustomsInfo
                {
                    ErrorMessage = "Failed to get customs information"
                };
            }
        }

        public async Task<DeliveryEstimate> GetDeliveryEstimateAsync(Models.LogisticsRequest request)
        {
            try
            {
                var estimate = new DeliveryEstimate
                {
                    FromCountry = request.OriginCountry,
                    ToCountry = request.DestinationCountry,
                    ShippingMethod = ParseLogisticsType(request.ShippingMethod)
                };

                // Calculate delivery time based on shipping method and distance
                switch (ParseLogisticsType(request.ShippingMethod))
                {
                    case LogisticsType.Air:
                        estimate.EstimatedDays = 3;
                        estimate.ServiceLevel = "Express";
                        break;
                    case LogisticsType.Sea:
                        estimate.EstimatedDays = 21;
                        estimate.ServiceLevel = "Standard";
                        break;
                    case LogisticsType.Land:
                        estimate.EstimatedDays = 7;
                        estimate.ServiceLevel = "Ground";
                        break;
                    case LogisticsType.Express:
                        estimate.EstimatedDays = 1;
                        estimate.ServiceLevel = "Premium";
                        break;
                    default:
                        estimate.EstimatedDays = 5;
                        estimate.ServiceLevel = "Standard";
                        break;
                }

                estimate.EstimatedDeliveryDate = DateTime.UtcNow.AddDays(estimate.EstimatedDays);
                estimate.CreatedDate = DateTime.UtcNow;

                return estimate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting delivery estimate for request from {FromCountry} to {ToCountry}", 
                    request.OriginCountry, request.DestinationCountry);
                return new DeliveryEstimate
                {
                    ErrorMessage = "Failed to get delivery estimate"
                };
            }
        }

        public async Task<List<Carrier>> GetAvailableCarriersAsync(string fromCountry, string toCountry)
        {
            try
            {
                var carriers = new List<Carrier>();

                // Global carriers
                carriers.Add(new Carrier
                {
                    Name = "DHL",
                    Code = "DHL",
                    Services = new[] { LogisticsType.Air, LogisticsType.Express },
                    Coverage = "Global",
                    Rating = 4.5m
                });

                carriers.Add(new Carrier
                {
                    Name = "FedEx",
                    Code = "FEDEX",
                    Services = new[] { LogisticsType.Air, LogisticsType.Express },
                    Coverage = "Global",
                    Rating = 4.3m
                });

                carriers.Add(new Carrier
                {
                    Name = "UPS",
                    Code = "UPS",
                    Services = new[] { LogisticsType.Air, LogisticsType.Land },
                    Coverage = "Global",
                    Rating = 4.2m
                });

                // Regional carriers
                if (IsSameContinent(fromCountry, toCountry))
                {
                    carriers.Add(new Carrier
                    {
                        Name = "Regional Express",
                        Code = "REGIONAL",
                        Services = new[] { LogisticsType.Land, LogisticsType.Air },
                        Coverage = "Regional",
                        Rating = 4.0m
                    });
                }

                return carriers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available carriers from {FromCountry} to {ToCountry}", 
                    fromCountry, toCountry);
                return new List<Carrier>();
            }
        }

        public async Task<LogisticsQuote> GetBulkShippingQuoteAsync(List<Models.LogisticsRequest> requests)
        {
            try
            {
                var totalWeight = requests.Sum(r => r.Weight);
                var totalVolume = requests.Sum(r => (r.Length ?? 0) * (r.Width ?? 0) * (r.Height ?? 0));

                // Create bulk request
                var bulkRequest = new Models.LogisticsRequest
                {
                    OriginCountry = requests.First().OriginCountry,
                    OriginCity = requests.First().OriginCity,
                    DestinationCountry = requests.First().DestinationCountry,
                    DestinationCity = requests.First().DestinationCity,
                    Weight = totalWeight,
                    Length = requests.Max(r => r.Length ?? 0),
                    Width = requests.Max(r => r.Width ?? 0),
                    Height = requests.Sum(r => r.Height ?? 0),
                    ShippingMethod = "Sea", // Bulk typically uses sea freight
                    DeclaredValue = requests.Sum(r => r.DeclaredValue ?? 0)
                };

                var quote = await CalculateShippingCostAsync(bulkRequest);
                
                // Apply bulk discount
                quote.BulkDiscount = quote.TotalCost * 0.15m; // 15% bulk discount
                quote.TotalCost -= quote.BulkDiscount;

                return quote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bulk shipping quote for {RequestCount} requests", requests.Count);
                return new LogisticsQuote
                {
                    ErrorMessage = "Failed to get bulk shipping quote"
                };
            }
        }

        private async Task<decimal> CalculateBaseCostAsync(Models.LogisticsRequest request)
        {
            // Simulate distance calculation and base cost
            var distance = CalculateDistance(request.OriginCountry, request.DestinationCountry);
            var weightFactor = request.Weight / 1000m; // Cost per kg
            var volumeFactor = ((request.Length ?? 0) * (request.Width ?? 0) * (request.Height ?? 0)) / 1000000m; // Cost per cubic meter

            var baseCost = distance * weightFactor * 0.5m + distance * volumeFactor * 0.3m;
            
            // Apply shipping method multiplier
            switch (ParseLogisticsType(request.ShippingMethod))
            {
                case LogisticsType.Air:
                    baseCost *= 3.0m;
                    break;
                case LogisticsType.Sea:
                    baseCost *= 0.8m;
                    break;
                case LogisticsType.Express:
                    baseCost *= 4.0m;
                    break;
                default:
                    baseCost *= 1.5m;
                    break;
            }

            return Math.Max(50m, baseCost); // Minimum cost of $50
        }

        private async Task<decimal> CalculateCustomsFeesAsync(Models.LogisticsRequest request)
        {
            if (request.OriginCountry == request.DestinationCountry)
                return 0m;

            var customsInfo = await GetCustomsInformationAsync(request.OriginCountry, request.DestinationCountry, "0701.90");
            return (request.DeclaredValue ?? 0) * (customsInfo.DutyRate + customsInfo.TaxRate);
        }

        private async Task<int> CalculateDeliveryTimeAsync(Models.LogisticsRequest request)
        {
            switch (ParseLogisticsType(request.ShippingMethod))
            {
                case LogisticsType.Air:
                    return 3;
                case LogisticsType.Sea:
                    return 21;
                case LogisticsType.Land:
                    return 7;
                case LogisticsType.Express:
                    return 1;
                default:
                    return 5;
            }
        }

        private async Task<string> GetCarrierForRouteAsync(Models.LogisticsRequest request)
        {
            var carriers = await GetAvailableCarriersAsync(request.OriginCountry, request.DestinationCountry);
            return carriers.FirstOrDefault()?.Name ?? "Standard Carrier";
        }

        private string GetServiceLevel(LogisticsType shippingMethod)
        {
            return shippingMethod switch
            {
                LogisticsType.Express => "Premium",
                LogisticsType.Air => "Express",
                LogisticsType.Sea => "Standard",
                LogisticsType.Land => "Ground",
                _ => "Standard"
            };
        }

        private bool IsSameContinent(string country1, string country2)
        {
            // Simplified continent check
            var continents = new Dictionary<string, string>
            {
                ["US"] = "North America", ["CA"] = "North America", ["MX"] = "North America",
                ["GB"] = "Europe", ["DE"] = "Europe", ["FR"] = "Europe", ["IT"] = "Europe",
                ["CN"] = "Asia", ["JP"] = "Asia", ["IN"] = "Asia", ["KR"] = "Asia",
                ["AU"] = "Oceania", ["NZ"] = "Oceania",
                ["BR"] = "South America", ["AR"] = "South America",
                ["ZA"] = "Africa", ["EG"] = "Africa", ["NG"] = "Africa"
            };

            return continents.TryGetValue(country1, out var continent1) &&
                   continents.TryGetValue(country2, out var continent2) &&
                   continent1 == continent2;
        }

        private decimal CalculateDistance(string fromCountry, string toCountry)
        {
            // Simplified distance calculation
            var distances = new Dictionary<string, Dictionary<string, decimal>>
            {
                ["US"] = new Dictionary<string, decimal>
                {
                    ["CA"] = 1000, ["MX"] = 2000, ["GB"] = 5000, ["CN"] = 8000
                },
                ["GB"] = new Dictionary<string, decimal>
                {
                    ["DE"] = 500, ["FR"] = 300, ["US"] = 5000, ["CN"] = 7000
                },
                ["CN"] = new Dictionary<string, decimal>
                {
                    ["JP"] = 1000, ["KR"] = 800, ["US"] = 8000, ["GB"] = 7000
                }
            };

            if (distances.TryGetValue(fromCountry, out var fromDistances) &&
                fromDistances.TryGetValue(toCountry, out var distance))
            {
                return distance;
            }

            return 5000m; // Default distance
        }

        private LogisticsType ParseLogisticsType(string shippingMethod)
        {
            return shippingMethod?.ToLower() switch
            {
                "air" => LogisticsType.Air,
                "sea" => LogisticsType.Sea,
                "land" => LogisticsType.Land,
                "express" => LogisticsType.Express,
                _ => LogisticsType.Air // Default to Air
            };
        }
    }



    public class LogisticsQuote
    {
        public string RequestId { get; set; }
        public string FromCountry { get; set; }
        public string FromCity { get; set; }
        public string ToCountry { get; set; }
        public string ToCity { get; set; }
        public decimal Weight { get; set; }
        public string WeightUnit { get; set; }
        public Dimensions Dimensions { get; set; }
        public LogisticsType ShippingMethod { get; set; }
        public decimal BaseCost { get; set; }
        public decimal FuelSurcharge { get; set; }
        public decimal CustomsFees { get; set; }
        public decimal InsuranceCost { get; set; }
        public decimal BulkDiscount { get; set; }
        public decimal TotalCost { get; set; }
        public string Currency { get; set; }
        public int EstimatedDays { get; set; }
        public string Carrier { get; set; }
        public string ServiceLevel { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class LogisticsTracking
    {
        public string TrackingNumber { get; set; }
        public string Carrier { get; set; }
        public string Status { get; set; }
        public string CurrentLocation { get; set; }
        public DateTime EstimatedDelivery { get; set; }
        public DateTime LastUpdate { get; set; }
        public List<TrackingEvent> Events { get; set; } = new List<TrackingEvent>();
        public string ErrorMessage { get; set; }
    }

    public class TrackingEvent
    {
        public DateTime Timestamp { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
    }

    public class ShippingMethod
    {
        public LogisticsType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int EstimatedDays { get; set; }
        public decimal MaxWeight { get; set; }
        public Dimensions MaxDimensions { get; set; }
    }

    public class CustomsInfo
    {
        public string FromCountry { get; set; }
        public string ToCountry { get; set; }
        public string HSCode { get; set; }
        public decimal DutyRate { get; set; }
        public decimal TaxRate { get; set; }
        public bool RequiresLicense { get; set; }
        public bool RestrictedItems { get; set; }
        public List<string> DocumentationRequired { get; set; } = new List<string>();
        public string ErrorMessage { get; set; }
    }

    public class DeliveryEstimate
    {
        public string FromCountry { get; set; }
        public string ToCountry { get; set; }
        public LogisticsType ShippingMethod { get; set; }
        public int EstimatedDays { get; set; }
        public DateTime EstimatedDeliveryDate { get; set; }
        public string ServiceLevel { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class Carrier
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public LogisticsType[] Services { get; set; }
        public string Coverage { get; set; }
        public decimal Rating { get; set; }
    }

    public class Dimensions
    {
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
    }
} 