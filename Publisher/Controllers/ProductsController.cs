using Application.DTOs;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;

namespace Publisher.Controllers;

/// <summary>
/// API controller for publishing product messages to RabbitMQ.
/// Endpoints: POST /api/products (create), PUT /api/products/{id} (update), GET /api/products/create (generate fake and create)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : PublisherBaseController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProductsController"/> class.
    /// </summary>
    /// <param name="connectionFactory">The RabbitMQ connection factory.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    public ProductsController(IConnectionFactory connectionFactory, ILogger<ProductsController> logger)
        : base(connectionFactory, logger)
    {
    }

    /// <summary>
    /// Generates a fake product using Bogus and publishes a create message to RabbitMQ.
    /// </summary>
    /// <returns>Generated product response</returns>
    [HttpGet("create")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> CreateFakeProduct()
    {
        _logger.LogInformation("=== GET /api/products/create - Generate Fake Product ===");

        try
        {
            _logger.LogDebug("Generating fake product using Bogus...");

            var faker = new Faker();
            var productCode = Guid.NewGuid();
            var productName = faker.Commerce.ProductName();

            _logger.LogInformation("Generated fake product - Code: {ProductCode}, Name: {Name}", productCode, productName);

            _logger.LogDebug("Creating product message payload...");
            // Create message payload from generated data
            var message = new
            {
                instruction = "create",
                entityType = "Product",
                payload = new
                {
                    productCode = productCode.ToString(),
                    name = productName
                }
            };
            _logger.LogDebug("Message payload created successfully");

            _logger.LogInformation("Publishing message to RabbitMQ...");
            await PublishMessageAsync("messaging_exchange", "product.create", message);
            _logger.LogInformation("Message published to RabbitMQ: Instruction=create, EntityType=Product, RoutingKey=product.create");

            // Return accepted response
            var responseDto = new ProductDto
            {
                Id = 0, // ID will be assigned by the Subscriber in the database
                ProductCode = productCode,
                Name = productName
            };

            _logger.LogInformation("=== Fake Product Message Published Successfully ===");
            return Accepted(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== ERROR Publishing Fake Product Message ===");
            _logger.LogError("Error details: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new product and publishes a create message to RabbitMQ.
    /// </summary>
    /// <param name="createDto">The product data transfer object</param>
    /// <returns>Created product response</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createDto)
    {
        try
        {
            _logger.LogInformation("=== POST /api/products - Create Product ===");
            _logger.LogInformation("Received request to create product: Code={ProductCode}, Name={ProductName}", createDto.ProductCode, createDto.Name);

            _logger.LogDebug("Creating product message payload...");
            // Create message payload from DTO
            var message = new
            {
                instruction = "create",
                entityType = "Product",
                payload = new
                {
                    productCode = createDto.ProductCode,
                    name = createDto.Name
                }
            };

            _logger.LogInformation("Publishing message to RabbitMQ...");
            await PublishMessageAsync("messaging_exchange", "product.create", message);
            _logger.LogInformation("Message published to RabbitMQ: Instruction=create, EntityType=Product, RoutingKey=product.create");

            // Return accepted response
            var responseDto = new ProductDto
            {
                Id = 0, // ID will be assigned by the Subscriber in the database
                ProductCode = createDto.ProductCode,
                Name = createDto.Name
            };

            return Accepted(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error publishing product creation message: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing product and publishes an update message to RabbitMQ.
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <param name="updateDto">The updated product data transfer object</param>
    /// <returns>Updated product response</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(long id, [FromBody] UpdateProductDto updateDto)
    {
        try
        {
            _logger.LogInformation("=== PUT /api/products/{ProductId} - Update Product ===", id);
            _logger.LogInformation("Received request to update product: ID={ProductId}, Code={ProductCode}, Name={ProductName}", id, updateDto.ProductCode, updateDto.Name);

            _logger.LogDebug("Creating product update message payload...");
            // Create message payload from DTO
            var message = new
            {
                instruction = "update",
                entityType = "Product",
                id = id,
                payload = new
                {
                    productCode = updateDto.ProductCode,
                    name = updateDto.Name
                }
            };

            _logger.LogInformation("Publishing message to RabbitMQ...");
            await PublishMessageAsync("messaging_exchange", "product.update", message);
            _logger.LogInformation("Message published to RabbitMQ: Instruction=update, EntityType=Product, ID={ProductId}, RoutingKey=product.update", id);

            // Return accepted response
            var responseDto = new ProductDto
            {
                Id = id,
                ProductCode = updateDto.ProductCode,
                Name = updateDto.Name
            };

            return Accepted(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error publishing product update message: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }
}
