using Application.DTOs;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;

namespace Publisher.Controllers;

/// <summary>
/// API controller for publishing supplier messages to RabbitMQ.
/// Endpoints: POST /api/suppliers (create), PUT /api/suppliers/{id} (update), GET /api/suppliers/create (generate fake and create)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SuppliersController : PublisherBaseController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SuppliersController"/> class.
    /// </summary>
    /// <param name="connectionFactory">The RabbitMQ connection factory.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    public SuppliersController(IConnectionFactory connectionFactory, ILogger<SuppliersController> logger)
        : base(connectionFactory, logger)
    {
    }

    /// <summary>
    /// Generates a fake supplier using Bogus and publishes a create message to RabbitMQ.
    /// </summary>
    /// <returns>Generated supplier response</returns>
    [HttpGet("create")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupplierDto>> CreateFakeSupplier()
    {
        _logger.LogInformation("=== GET /api/suppliers/create - Generate Fake Supplier ===");

        try
        {
            _logger.LogDebug("Generating fake supplier using Bogus...");

            var faker = new Faker();
            var supplierName = faker.Company.CompanyName();

            _logger.LogInformation("Generated fake supplier - Name: {Name}", supplierName);

            _logger.LogDebug("Creating supplier message payload...");
            // Create message payload from generated data
            var message = new
            {
                instruction = "create",
                entityType = "Supplier",
                payload = new
                {
                    name = supplierName
                }
            };
            _logger.LogDebug("Message payload created successfully");

            _logger.LogInformation("Publishing message to RabbitMQ...");
            await PublishMessageAsync("messaging_exchange", "supplier.create", message);
            _logger.LogInformation("Message published to RabbitMQ: Instruction=create, EntityType=Supplier, RoutingKey=supplier.create");

            // Return accepted response
            var responseDto = new SupplierDto
            {
                Id = 0, // ID will be assigned by the Subscriber in the database
                Name = supplierName
            };

            _logger.LogInformation("=== Fake Supplier Message Published Successfully ===");
            return Accepted(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== ERROR Publishing Fake Supplier Message ===");
            _logger.LogError("Error details: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new supplier and publishes a create message to RabbitMQ.
    /// </summary>
    /// <param name="createDto">The supplier data transfer object</param>
    /// <returns>Created supplier response</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupplierDto>> CreateSupplier([FromBody] CreateSupplierDto createDto)
    {
        try
        {
            _logger.LogInformation("=== POST /api/suppliers - Create Supplier ===");
            _logger.LogInformation("Received request to create supplier: Name={SupplierName}", createDto.Name);

            _logger.LogDebug("Creating supplier message payload...");
            // Create message payload from DTO
            var message = new
            {
                instruction = "create",
                entityType = "Supplier",
                payload = new
                {
                    name = createDto.Name
                }
            };

            _logger.LogInformation("Publishing message to RabbitMQ...");
            await PublishMessageAsync("messaging_exchange", "supplier.create", message);
            _logger.LogInformation("Message published to RabbitMQ: Instruction=create, EntityType=Supplier, RoutingKey=supplier.create");

            // Return accepted response
            var responseDto = new SupplierDto
            {
                Id = 0, // ID will be assigned by the Subscriber in the database
                Name = createDto.Name
            };

            return Accepted(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error publishing supplier creation message: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing supplier and publishes an update message to RabbitMQ.
    /// </summary>
    /// <param name="id">The supplier ID</param>
    /// <param name="updateDto">The updated supplier data transfer object</param>
    /// <returns>Updated supplier response</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(long id, [FromBody] UpdateSupplierDto updateDto)
    {
        try
        {
            _logger.LogInformation("=== PUT /api/suppliers/{SupplierId} - Update Supplier ===", id);
            _logger.LogInformation("Received request to update supplier: ID={SupplierId}, Name={SupplierName}", id, updateDto.Name);

            _logger.LogDebug("Creating supplier update message payload...");
            // Create message payload from DTO
            var message = new
            {
                instruction = "update",
                entityType = "Supplier",
                id = id,
                payload = new
                {
                    name = updateDto.Name
                }
            };

            _logger.LogInformation("Publishing message to RabbitMQ...");
            await PublishMessageAsync("messaging_exchange", "supplier.update", message);
            _logger.LogInformation("Message published to RabbitMQ: Instruction=update, EntityType=Supplier, ID={SupplierId}, RoutingKey=supplier.update", id);

            // Return accepted response
            var responseDto = new SupplierDto
            {
                Id = id,
                Name = updateDto.Name
            };

            return Accepted(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error publishing supplier update message: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }
}
