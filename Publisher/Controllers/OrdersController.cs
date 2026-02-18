using Application.DTOs;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using Domain.Models;
using Infrastructure.Repositories;

namespace Publisher.Controllers;

/// <summary>
/// API controller for publishing order messages to RabbitMQ.
/// Endpoints: POST /api/orders (create), PUT /api/orders/{id} (update), GET /api/orders/create (generate fake and create)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : PublisherBaseController
{
    private readonly IProductRepository _productRepository;
    private Dictionary<long, Domain.Models.Product> _productsCache = new();
    private bool _productsCacheLoaded = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrdersController"/> class.
    /// </summary>
    /// <param name="connectionFactory">The RabbitMQ connection factory.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <param name="productRepository">The product repository for database access.</param>
    public OrdersController(IConnectionFactory connectionFactory, ILogger<OrdersController> logger, IProductRepository productRepository)
        : base(connectionFactory, logger)
    {
        _productRepository = productRepository;
    }

    /// <summary>
    /// Loads all products into a cache dictionary for efficient lookups.
    /// </summary>
    private async Task InitializeProductsCacheAsync()
    {
        if (_productsCacheLoaded)
        {
            _logger.LogDebug("Products cache already loaded, skipping initialization");
            return;
        }

        _logger.LogInformation("Initializing products cache...");
        try
        {
            // Get all products from repository
            var allProducts = await _productRepository.GetAllAsync();

            _productsCache.Clear();
            foreach (var product in allProducts)
            {
                _productsCache[product.Id] = product;
            }

            _productsCacheLoaded = true;
            _logger.LogInformation("✓ Products cache initialized with {ProductCount} products", _productsCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing products cache");
            _productsCacheLoaded = false;
            throw;
        }
    }

    /// <summary>
    /// Generates a fake order using Bogus and publishes a create message to RabbitMQ.
    /// </summary>
    /// <returns>Generated order response</returns>
    [HttpGet("create")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> CreateFakeOrder()
    {
        _logger.LogInformation("=== GET /api/orders/create - Generate Fake Order ===");

        try
        {
            // Initialize products cache on first use
            await InitializeProductsCacheAsync();

            _logger.LogDebug("Generating fake order using Bogus (UK locale)...");

            var faker = new Faker("en_GB");  // Set locale to United Kingdom

            // Generate valid data according to validation rules
            var customerId = faker.Random.Long(1, 100);  // CustomerId between 1-100
            var supplierId = faker.PickRandom(1L, 2L);  // SupplierId must be 1 or 2
            var orderDate = DateTime.Now;  // Current date/time for easy identification in DB
            var customerEmail = faker.Internet.Email();
            var billingAddress = new
            {
                street = faker.Address.StreetAddress(),
                city = faker.Address.City(),
                county = faker.Address.County(),
                postalCode = faker.Address.ZipCode(),
                country = faker.Address.Country()
            };
            var deliveryAddress = new
            {
                street = faker.Address.StreetAddress(),
                city = faker.Address.City(),
                county = faker.Address.County(),
                postalCode = faker.Address.ZipCode(),
                country = faker.Address.Country()
            };

            // OrderStatus = 0 (Received state - default)
            var orderStatusValue = "Received";  // Set to Received status (value = 0)

            _logger.LogInformation("Generated fake order - CustomerId: {CustomerId}, SupplierId: {SupplierId}, OrderDate: {OrderDate}, Status: {Status}", 
                customerId, supplierId, orderDate, orderStatusValue);

            // Generate multiple fake order items (2-4 items)
            var itemCount = faker.Random.Int(2, 4);
            var orderItems = new object[itemCount];

            for (int i = 0; i < itemCount; i++)
            {
                orderItems[i] = new
                {
                    productId = faker.Random.Long(1, 50),
                    quantity = faker.Random.Int(1, 10),
                    unitPrice = decimal.Parse(faker.Commerce.Price(10, 1000))
                };
            }

            _logger.LogDebug("Creating order message payload with {ItemCount} fake order item(s)...", orderItems.Length);

            // Log each generated item details
            for (int i = 0; i < orderItems.Length; i++)
            {
                dynamic item = orderItems[i];
                long productId = item.productId;
                int quantity = item.quantity;
                decimal unitPrice = item.unitPrice;
                _logger.LogDebug("Order Item {Index}: ProductId={ProductId}, Quantity={Quantity}, UnitPrice={UnitPrice}", 
                    i + 1, productId, quantity, unitPrice);
            }

            // Enrich order items using cached products
            _logger.LogDebug("Enriching order items with product details from cache...");
            for (int i = 0; i < orderItems.Length; i++)
            {
                dynamic item = orderItems[i];
                long productId = item.productId;

                if (_productsCache.TryGetValue(productId, out var product))
                {
                    // Create enriched item with product details from cache
                    orderItems[i] = new
                    {
                        productId = item.productId,
                        quantity = item.quantity,
                        unitPrice = item.unitPrice,
                        productName = product.Name,
                        productCode = product.ProductCode.ToString()
                    };

                    _logger.LogDebug("Product found in cache - ID: {ProductId}, Name: {ProductName}, Code: {ProductCode}",
                        product.Id, product.Name, product.ProductCode);
                }
                else
                {
                    _logger.LogWarning("Product not found in cache - ProductId: {ProductId}", productId);

                    // Create enriched item with empty product details
                    orderItems[i] = new
                    {
                        productId = item.productId,
                        quantity = item.quantity,
                        unitPrice = item.unitPrice,
                        productName = string.Empty,
                        productCode = string.Empty
                    };
                }
            }

            // Create message payload from generated data
            var message = new
            {
                instruction = "create",
                entityType = "Order",
                payload = new
                {
                    customerId = customerId,
                    supplierId = supplierId,
                    orderDate = orderDate,
                    customerEmail = customerEmail,
                    orderStatus = orderStatusValue,
                    billingAddress = billingAddress,
                    deliveryAddress = deliveryAddress,
                    orderItems = orderItems
                }
            };
            _logger.LogDebug("Message payload created successfully with billing and delivery addresses");

            _logger.LogInformation("Publishing message to RabbitMQ...");
            await PublishMessageAsync("messaging_exchange", "order.create", message);
            _logger.LogInformation("Message published to RabbitMQ: Instruction=create, EntityType=Order, RoutingKey=order.create");

            // Return accepted response
            var orderItemDtos = new List<OrderItemDto>();
            for (int i = 0; i < orderItems.Length; i++)
            {
                dynamic item = orderItems[i];
                orderItemDtos.Add(new OrderItemDto
                {
                    ProductId = item.productId,
                    ProductName = item.productName ?? string.Empty,
                    ProductCode = item.productCode != string.Empty ? Guid.Parse(item.productCode) : Guid.Empty,
                    Quantity = item.quantity,
                    Price = item.unitPrice
                });
            }

            var responseDto = new OrderDto
            {
                Id = 0, // ID will be assigned by the Subscriber in the database
                CustomerId = customerId,
                SupplierId = supplierId,
                OrderDate = orderDate,
                CustomerEmail = customerEmail,
                BillingAddress = new AddressDto 
                { 
                    Street = faker.Address.StreetAddress(),
                    City = faker.Address.City(),
                    County = faker.Address.County(),
                    PostalCode = faker.Address.ZipCode(),
                    Country = faker.Address.Country()
                },
                DeliveryAddress = new AddressDto 
                { 
                    Street = faker.Address.StreetAddress(),
                    City = faker.Address.City(),
                    County = faker.Address.County(),
                    PostalCode = faker.Address.ZipCode(),
                    Country = faker.Address.Country()
                },
                OrderStatus = OrderStatus.Received,
                OrderItems = orderItemDtos
            };

            _logger.LogInformation("=== Fake Order Message Published Successfully ===");
            return Accepted(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== ERROR Publishing Fake Order Message ===");
            _logger.LogError("Error details: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new order and publishes a create message to RabbitMQ.
    /// </summary>
    /// <param name="createDto">The order data transfer object</param>
    /// <returns>Created order response</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto createDto)
    {
        try
        {
            _logger.LogInformation("=== POST /api/orders - Create Order ===");
            _logger.LogInformation("Received request to create order: CustomerId={CustomerId}, OrderDate={OrderDate}", createDto.CustomerId, createDto.OrderDate);

            // Validate order items - must have at least one
            if (createDto.OrderItems == null || createDto.OrderItems.Count == 0)
            {
                _logger.LogWarning("Invalid order creation request: OrderItems is null or empty");
                return BadRequest(new { error = "Order must contain at least one order item" });
            }

            _logger.LogDebug("Order contains {ItemCount} item(s)", createDto.OrderItems.Count);
            _logger.LogDebug("Creating order message payload...");

            // Create message payload from DTO
            var message = new
            {
                instruction = "create",
                entityType = "Order",
                payload = new
                {
                    customerId = createDto.CustomerId,
                    supplierId = createDto.SupplierId,
                    orderDate = createDto.OrderDate,
                    customerEmail = createDto.CustomerEmail,
                    orderStatus = createDto.OrderStatus.ToString(),
                    billingAddress = createDto.BillingAddress,
                    deliveryAddress = createDto.DeliveryAddress,
                    orderItems = createDto.OrderItems
                }
            };

            _logger.LogInformation("Publishing message to RabbitMQ...");
            await PublishMessageAsync("messaging_exchange", "order.create", message);
            _logger.LogInformation("Message published to RabbitMQ: Instruction=create, EntityType=Order, RoutingKey=order.create");

            // Return success response with product details populated from database
            var orderItemDtos = new List<OrderItemDto>();
            foreach (var item in createDto.OrderItems)
            {
                _logger.LogDebug("Fetching product details for ProductId: {ProductId}", item.ProductId);
                var product = await _productRepository.GetByIdAsync(item.ProductId);

                var orderItemDto = new OrderItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = product?.Name ?? string.Empty,
                    ProductCode = product?.ProductCode ?? Guid.Empty,
                    Quantity = item.Quantity,
                    Price = item.Price
                };

                if (product != null)
                {
                    _logger.LogDebug("Product found - ID: {ProductId}, Name: {ProductName}, Code: {ProductCode}", 
                        product.Id, product.Name, product.ProductCode);
                }
                else
                {
                    _logger.LogWarning("Product not found in database - ProductId: {ProductId}", item.ProductId);
                }

                orderItemDtos.Add(orderItemDto);
            }

            var responseDto = new OrderDto
            {
                Id = 0, // ID will be assigned by the Subscriber in the database
                CustomerId = createDto.CustomerId,
                SupplierId = createDto.SupplierId,
                SupplierName = string.Empty,
                OrderDate = createDto.OrderDate,
                CustomerEmail = createDto.CustomerEmail,
                OrderStatus = createDto.OrderStatus,
                TotalAmount = 0,
                OrderItems = orderItemDtos
            };

            return Accepted(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error publishing order creation message: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing order and publishes an update message to RabbitMQ.
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <param name="updateDto">The updated order data transfer object</param>
    /// <returns>Updated order response</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> UpdateOrder(long id, [FromBody] UpdateOrderDto updateDto)
    {
        try
        {
            _logger.LogInformation("=== PUT /api/orders/{OrderId} - Update Order ===", id);
            _logger.LogInformation("Received request to update order: ID={OrderId}, CustomerId={CustomerId}", id, updateDto.CustomerId);

            _logger.LogDebug("Creating order update message payload...");
            // Create message payload from DTO
            var message = new
            {
                instruction = "update",
                entityType = "Order",
                id = id,
                payload = new
                {
                    customerId = updateDto.CustomerId,
                    supplierId = updateDto.SupplierId,
                    orderDate = updateDto.OrderDate,
                    customerEmail = updateDto.CustomerEmail,
                    orderStatus = updateDto.OrderStatus.ToString(),
                    billingAddress = updateDto.BillingAddress,
                    deliveryAddress = updateDto.DeliveryAddress
                }
            };

            _logger.LogInformation("Publishing message to RabbitMQ...");
            await PublishMessageAsync("messaging_exchange", "order.update", message);
            _logger.LogInformation("Message published to RabbitMQ: Instruction=update, EntityType=Order, ID={OrderId}, RoutingKey=order.update", id);

            // Return accepted response
            var responseDto = new OrderDto
            {
                Id = id,
                CustomerId = updateDto.CustomerId,
                SupplierId = updateDto.SupplierId,
                SupplierName = string.Empty,
                OrderDate = updateDto.OrderDate,
                CustomerEmail = updateDto.CustomerEmail,
                OrderStatus = updateDto.OrderStatus,
                TotalAmount = 0
            };

            return Accepted(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error publishing order update message: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }
}
