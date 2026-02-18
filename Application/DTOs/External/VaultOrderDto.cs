using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.External
{
    /// <summary>
    /// External order format from Vault supplier.
    /// This format is completely different from Speedy's format.
    /// Teaching point: Shows how different suppliers may have vastly different API formats.
    /// Vault uses email for customer identification and GUIDs for product codes.
    /// </summary>
    public class VaultOrderDto
    {
        /// <summary>
        /// Vault uses "customer_email" for customer identification (REQUIRED)
        /// </summary>
        [Required(ErrorMessage = "CustomerEmail is required for Vault orders.")]
        [EmailAddress(ErrorMessage = "CustomerEmail must be a valid email address.")]
        public string CustomerEmail { get; set; } = string.Empty;

        /// <summary>
        /// Vault uses "placed_at" with Unix timestamp
        /// </summary>
        [Required]
        public long PlacedAt { get; set; } // Unix timestamp

        /// <summary>
        /// Vault uses nested "delivery_details" object
        /// </summary>
        public VaultDeliveryDetailsDto? DeliveryDetails { get; set; }

        /// <summary>
        /// Vault uses "items" array with nested product info
        /// </summary>
        [Required]
        [MinLength(1)]
        public List<VaultItemDto> Items { get; set; } = new();

        /// <summary>
        /// Vault includes fulfillment instructions
        /// </summary>
        public string? FulfillmentInstructions { get; set; }
    }

    /// <summary>
    /// Vault's delivery details format - includes both addresses
    /// </summary>
    public class VaultDeliveryDetailsDto
    {
        public VaultLocationDto? BillingLocation { get; set; }
        public VaultLocationDto? ShippingLocation { get; set; }
    }

    /// <summary>
    /// Vault's location (address) format
    /// </summary>
    public class VaultLocationDto
    {
        public string? AddressLine { get; set; }
        public string? CityName { get; set; }
        public string? StateProvince { get; set; }
        public string? ZipPostal { get; set; }
        public string? CountryCode { get; set; } // ISO country code
    }

    /// <summary>
    /// Vault's item format
    /// </summary>
    public class VaultItemDto
    {
        /// <summary>
        /// Vault uses "product_code" as GUID identifier
        /// </summary>
        [Required]
        public Guid ProductCode { get; set; }

        /// <summary>
        /// Vault uses "quantity_ordered"
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int QuantityOrdered { get; set; }

        /// <summary>
        /// Vault uses "price_per_unit"
        /// </summary>
        [Required]
        [Range(0, double.MaxValue)]
        public decimal PricePerUnit { get; set; }
    }
}

