using System.ComponentModel.DataAnnotations;
using Domain.Models;

namespace Application.DTOs
{
    public class AddressDto
    {
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? County { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }

    public class OrderDto
    {
        public long Id { get; set; }
        public long? CustomerId { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string? CustomerEmail { get; set; }
        public AddressDto? BillingAddress { get; set; }
        public AddressDto? DeliveryAddress { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemDto>? OrderItems { get; set; }
    }

    public class OrderItemDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public Guid ProductCode { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal LineTotal => Quantity * Price;
    }

    public class CreateOrderDto
    {
        public long? CustomerId { get; set; }

        [Required(ErrorMessage = "SupplierId is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "SupplierId must be greater than zero.")]
        public long SupplierId { get; set; }

        [Required(ErrorMessage = "OrderDate is required.")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [StringLength(200, ErrorMessage = "CustomerEmail cannot exceed 200 characters.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string? CustomerEmail { get; set; }

        public AddressDto? BillingAddress { get; set; }

        public AddressDto? DeliveryAddress { get; set; }

        [Required(ErrorMessage = "OrderStatus is required.")]
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Received;

        [Required(ErrorMessage = "OrderItems is required.")]
        [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
        public List<CreateOrderItemDto> OrderItems { get; set; } = new List<CreateOrderItemDto>();
    }

    public class CreateOrderItemDto
    {
        [Required(ErrorMessage = "ProductId is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "ProductId must be greater than zero.")]
        public long ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0.")]
        public decimal Price { get; set; }
    }

    public class UpdateOrderDto
    {
        public long? CustomerId { get; set; }

        [Required(ErrorMessage = "SupplierId is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "SupplierId must be greater than zero.")]
        public long SupplierId { get; set; }

        [Required(ErrorMessage = "OrderDate is required.")]
        public DateTime OrderDate { get; set; }

        [StringLength(200, ErrorMessage = "CustomerEmail cannot exceed 200 characters.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string? CustomerEmail { get; set; }

        public AddressDto? BillingAddress { get; set; }

        public AddressDto? DeliveryAddress { get; set; }

        [Required(ErrorMessage = "OrderStatus is required.")]
        public OrderStatus OrderStatus { get; set; }

        [Required(ErrorMessage = "OrderItems is required.")]
        [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
        public List<CreateOrderItemDto> OrderItems { get; set; } = new List<CreateOrderItemDto>();
    }
}
