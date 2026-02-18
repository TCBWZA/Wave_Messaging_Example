using Domain.Models;
using Application.DTOs;

namespace Application.Mappings
{
    public static class MappingExtensions
    {
        // Customer mappings
        public static CustomerDto ToDto(this Customer customer)
        {
            return new CustomerDto
            {
                Id = customer.Id,
                Name = customer.Name ?? string.Empty,
                Email = customer.Email ?? string.Empty,
                PhoneNumbers = customer.PhoneNumbers?.Select(p => p.ToDto()).ToList()
            };
        }

        public static Customer ToEntity(this CreateCustomerDto dto)
        {
            return new Customer
            {
                Name = dto.Name,
                Email = dto.Email,
                PhoneNumbers = dto.PhoneNumbers?.Select(p => new TelephoneNumber
                {
                    Type = p.Type,
                    Number = p.Number
                }).ToList() ?? new List<TelephoneNumber>()
            };
        }

        public static void UpdateEntity(this UpdateCustomerDto dto, Customer customer)
        {
            customer.Name = dto.Name;
            customer.Email = dto.Email;
        }

        // Product mappings
        public static ProductDto ToDto(this Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                ProductCode = product.ProductCode,
                Name = product.Name
            };
        }

        public static Product ToEntity(this CreateProductDto dto)
        {
            return new Product
            {
                ProductCode = dto.ProductCode,
                Name = dto.Name
            };
        }

        public static void UpdateEntity(this UpdateProductDto dto, Product product)
        {
            product.ProductCode = dto.ProductCode;
            product.Name = dto.Name;
        }

        // TelephoneNumber mappings
        public static TelephoneNumberDto ToDto(this TelephoneNumber telephoneNumber)
        {
            return new TelephoneNumberDto
            {
                Id = telephoneNumber.Id,
                CustomerId = telephoneNumber.CustomerId,
                Type = telephoneNumber.Type,
                Number = telephoneNumber.Number
            };
        }

        public static TelephoneNumber ToEntity(this CreateTelephoneNumberDto dto)
        {
            return new TelephoneNumber
            {
                CustomerId = dto.CustomerId,
                Type = dto.Type,
                Number = dto.Number
            };
        }

        public static void UpdateEntity(this UpdateTelephoneNumberDto dto, TelephoneNumber telephoneNumber)
        {
            telephoneNumber.Type = dto.Type;
            telephoneNumber.Number = dto.Number;
        }

        // Supplier mappings
        public static SupplierDto ToDto(this Supplier supplier)
        {
            return new SupplierDto
            {
                Id = supplier.Id,
                Name = supplier.Name
            };
        }

        public static Supplier ToEntity(this CreateSupplierDto dto)
        {
            return new Supplier
            {
                Name = dto.Name
            };
        }

        public static void UpdateEntity(this UpdateSupplierDto dto, Supplier supplier)
        {
            supplier.Name = dto.Name;
        }

        // Order mappings
        public static AddressDto? ToDto(this Address? address)
        {
            if (address == null) return null;
            
            return new AddressDto
            {
                Street = address.Street,
                City = address.City,
                County = address.County,
                PostalCode = address.PostalCode,
                Country = address.Country
            };
        }

        public static Address? ToEntity(this AddressDto? dto)
        {
            if (dto == null) return null;
            
            return new Address
            {
                Street = dto.Street,
                City = dto.City,
                County = dto.County,
                PostalCode = dto.PostalCode,
                Country = dto.Country
            };
        }

        public static OrderDto ToDto(this Order order)
        {
            // TUTOR NOTE: Intentional Error - No null check for 'order' parameter.
            // If order is null, this will throw NullReferenceException instead of a meaningful error.
            // Also, BillingAddress and DeliveryAddress are called without null checks - line 156 & 157 will fail if they're null.
            // Students should write tests to verify: 1) Null order throws ArgumentNullException, 2) Null addresses are handled gracefully.
            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                SupplierId = order.SupplierId,
                SupplierName = order.Supplier?.Name ?? string.Empty,
                OrderDate = order.OrderDate,
                CustomerEmail = order.CustomerEmail,
                BillingAddress = order.BillingAddress.ToDto(),
                DeliveryAddress = order.DeliveryAddress.ToDto(),
                OrderStatus = order.OrderStatus,
                TotalAmount = order.TotalAmount,
                OrderItems = order.OrderItems?.Select(oi => oi.ToDto()).ToList()
            };
        }

        public static Order ToEntity(this CreateOrderDto dto)
        {
            return new Order
            {
                CustomerId = dto.CustomerId,
                SupplierId = dto.SupplierId,
                OrderDate = dto.OrderDate,
                CustomerEmail = dto.CustomerEmail,
                BillingAddress = dto.BillingAddress.ToEntity(),
                DeliveryAddress = dto.DeliveryAddress.ToEntity(),
                OrderStatus = dto.OrderStatus,
                OrderItems = dto.OrderItems?.Select(oi => new OrderItem
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList() ?? new List<OrderItem>()
            };
        }

        public static void UpdateEntity(this UpdateOrderDto dto, Order order)
        {
            order.CustomerId = dto.CustomerId;
            order.SupplierId = dto.SupplierId;
            order.OrderDate = dto.OrderDate;
            order.CustomerEmail = dto.CustomerEmail;
            order.BillingAddress = dto.BillingAddress.ToEntity();
            order.DeliveryAddress = dto.DeliveryAddress.ToEntity();
            order.OrderStatus = dto.OrderStatus;
            
            // Clear existing items and add new ones
            order.OrderItems?.Clear();
            order.OrderItems = dto.OrderItems?.Select(oi => new OrderItem
            {
                OrderId = order.Id,
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                Price = oi.Price
            }).ToList() ?? new List<OrderItem>();
        }

        // OrderItem mappings
        public static OrderItemDto ToDto(this OrderItem orderItem)
        {
            return new OrderItemDto
            {
                Id = orderItem.Id,
                ProductId = orderItem.ProductId,
                ProductName = orderItem.Product?.Name ?? string.Empty,
                ProductCode = orderItem.Product?.ProductCode ?? Guid.Empty,
                Quantity = orderItem.Quantity,
                Price = orderItem.Price
            };
        }
    }
}
