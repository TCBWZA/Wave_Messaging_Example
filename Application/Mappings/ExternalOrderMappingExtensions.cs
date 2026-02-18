using Application.DTOs.External;
using Domain.Models;

namespace Application.Mappings
{
    /// <summary>
    /// Extension methods to map external supplier formats to internal Order model.
    /// Teaching point: Shows how to normalize different external formats into your domain model.
    /// </summary>
    public static class ExternalOrderMappingExtensions
    {
        /// <summary>
        /// Maps Speedy's order format to internal Order model.
        /// Automatically sets SupplierId to Speedy (Id = 1).
        /// Speedy uses numeric CustomerId and ProductId.
        /// </summary>
        public static Order ToOrder(this SpeedyOrderDto dto)
        {
            return new Order
            {
                // Speedy uses numeric CustomerId (required)
                CustomerId = dto.CustomerId,
                CustomerEmail = null, // Speedy doesn't provide email

                // Automatically set supplier to Speedy (Id = 1)
                SupplierId = 1, // Speedy

                // Map order date
                OrderDate = dto.OrderTimestamp,

                // Set default status for new orders
                OrderStatus = OrderStatus.Received,

                // Map billing address (Speedy calls it "BillTo")
                BillingAddress = dto.BillTo != null ? new Address
                {
                    Street = dto.BillTo.StreetAddress,
                    City = dto.BillTo.City,
                    County = dto.BillTo.Region, // Speedy calls it "Region"
                    PostalCode = dto.BillTo.PostCode,
                    Country = dto.BillTo.Country
                } : null,

                // Map delivery address (Speedy calls it "ShipTo")
                DeliveryAddress = dto.ShipTo != null ? new Address
                {
                    Street = dto.ShipTo.StreetAddress,
                    City = dto.ShipTo.City,
                    County = dto.ShipTo.Region, // Speedy calls it "Region"
                    PostalCode = dto.ShipTo.PostCode,
                    Country = dto.ShipTo.Country
                } : null,

                // Map line items - Speedy uses numeric ProductId
                OrderItems = dto.LineItems?.Select(item => new OrderItem
                {
                    ProductId = item.ProductId, // Direct mapping - both use long
                    Quantity = item.Qty,  // Speedy calls it "Qty"
                    Price = item.UnitPrice // Speedy calls it "UnitPrice"
                }).ToList() ?? new List<OrderItem>()
            };
        }

        /// <summary>
        /// Maps Vault's order format to internal Order model.
        /// Automatically sets SupplierId to Vault (Id = 2).
        /// Vault uses CustomerEmail and ProductCode (Guid).
        /// 
        /// Note: This method performs basic mapping. When used in the Subscriber or Publisher,
        /// the ProductCode should be looked up via the API or repository to get the ProductId.
        /// This mapping stores the ProductCode and leaves ProductId resolution to be handled
        /// by the consuming layer (Subscriber or Publisher).
        /// </summary>
        public static Order ToOrder(this VaultOrderDto dto)
        {
            // Convert Unix timestamp to DateTime
            var orderDate = DateTimeOffset.FromUnixTimeSeconds(dto.PlacedAt).UtcDateTime;

            // Map items without ProductId lookup (will be resolved by consuming layer)
            var orderItems = dto.Items?.Select(item => new OrderItem
            {
                ProductId = 0, // Placeholder - must be resolved by repository lookup in consuming layer
                Quantity = item.QuantityOrdered,
                Price = item.PricePerUnit
            }).ToList() ?? new List<OrderItem>();

            return new Order
            {
                // Vault uses email for customer identification
                CustomerId = null, // Vault doesn't provide numeric ID
                CustomerEmail = dto.CustomerEmail,

                // Automatically set supplier to Vault (Id = 2)
                SupplierId = 2, // Vault

                // Map order date (convert from Unix timestamp)
                OrderDate = orderDate,

                // Set default status for new orders
                OrderStatus = OrderStatus.Received,

                // Map billing address (Vault uses nested structure)
                BillingAddress = dto.DeliveryDetails?.BillingLocation != null ? new Address
                {
                    Street = dto.DeliveryDetails.BillingLocation.AddressLine,
                    City = dto.DeliveryDetails.BillingLocation.CityName,
                    County = dto.DeliveryDetails.BillingLocation.StateProvince,
                    PostalCode = dto.DeliveryDetails.BillingLocation.ZipPostal,
                    Country = dto.DeliveryDetails.BillingLocation.CountryCode
                } : null,

                // Map delivery address
                DeliveryAddress = dto.DeliveryDetails?.ShippingLocation != null ? new Address
                {
                    Street = dto.DeliveryDetails.ShippingLocation.AddressLine,
                    City = dto.DeliveryDetails.ShippingLocation.CityName,
                    County = dto.DeliveryDetails.ShippingLocation.StateProvince,
                    PostalCode = dto.DeliveryDetails.ShippingLocation.ZipPostal,
                    Country = dto.DeliveryDetails.ShippingLocation.CountryCode
                } : null,

                // Use the mapped items
                OrderItems = orderItems
            };
        }
    }
}


