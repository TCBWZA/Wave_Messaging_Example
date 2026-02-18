using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.External
{
    /// <summary>
    /// External order format from Speedy supplier.
    /// This format uses different field names and structure than our internal Order model.
    /// Teaching point: Shows how to accept supplier-specific formats.
    /// Speedy uses numeric IDs for both customers and products.
    /// </summary>
    public class SpeedyOrderDto
    {
        /// <summary>
        /// Speedy uses "customer_id" - numeric customer identifier (REQUIRED)
        /// </summary>
        [Required(ErrorMessage = "CustomerId is required for Speedy orders.")]
        [Range(1, long.MaxValue, ErrorMessage = "CustomerId must be greater than zero.")]
        public long CustomerId { get; set; }

        /// <summary>
        /// Speedy uses "order_timestamp" in ISO format
        /// </summary>
        [Required]
        public DateTime OrderTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Speedy uses "ship_to" for delivery address
        /// </summary>
        public SpeedyAddressDto? ShipTo { get; set; }

        /// <summary>
        /// Speedy uses "bill_to" for billing address
        /// </summary>
        public SpeedyAddressDto? BillTo { get; set; }

        /// <summary>
        /// Speedy sends "line_items" array
        /// </summary>
        [Required]
        [MinLength(1)]
        public List<SpeedyLineItemDto> LineItems { get; set; } = new();

        /// <summary>
        /// Speedy includes priority level
        /// </summary>
        public string Priority { get; set; } = "standard"; // standard, express, overnight
    }

    /// <summary>
    /// Speedy's address format
    /// </summary>
    public class SpeedyAddressDto
    {
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; } // They call it Region instead of County
        public string? PostCode { get; set; }
        public string? Country { get; set; }
    }

    /// <summary>
    /// Speedy's line item format
    /// </summary>
    public class SpeedyLineItemDto
    {
        /// <summary>
        /// Speedy uses "product_id" as numeric product identifier
        /// </summary>
        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "ProductId must be greater than zero.")]
        public long ProductId { get; set; }

        /// <summary>
        /// Speedy uses "qty" instead of Quantity
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int Qty { get; set; }

        /// <summary>
        /// Speedy uses "unit_price" 
        /// </summary>
        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }
}

