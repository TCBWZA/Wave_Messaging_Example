using Application.DTOs;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;

namespace Publisher.Controllers;

/// <summary>
/// API controller for publishing customer messages to RabbitMQ.
/// Endpoints: POST /api/customers (create), PUT /api/customers/{id} (update), GET /api/customers/create (generate fake and create)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomersController : PublisherBaseController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomersController"/> class.
    /// </summary>
    /// <param name="connectionFactory">The RabbitMQ connection factory.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    public CustomersController(IConnectionFactory connectionFactory, ILogger<CustomersController> logger)
        : base(connectionFactory, logger)
    {
    }

    /// <summary>
    /// Generates a fake customer using Bogus and publishes a create message to RabbitMQ.
    /// </summary>
    /// <returns>Generated customer response</returns>
    [HttpGet("create")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerDto>> CreateFakeCustomer()
    {
        _logger.LogInformation("=== GET /api/customers/create - Generate Fake Customer ===");

        try
        {
            _logger.LogDebug("Generating fake customer using Bogus...");

            // Generate fake customer data
            var faker = new Faker();
            var phoneNumberFaker = new Faker<TelephoneNumberDto>()
                .RuleFor(p => p.Type, f => f.PickRandom("Work", "Mobile"))
                .RuleFor(p => p.Number, f => f.Phone.PhoneNumber("+1-###-###-####"));

            var customerName = faker.Company.CompanyName();
            var customerEmail = faker.Internet.Email();
            var phoneNumbers = phoneNumberFaker.Generate(faker.Random.Int(1, 3));

            _logger.LogInformation("Generated fake customer - Name: {Name}, Email: {Email}, PhoneCount: {PhoneCount}", 
                customerName, customerEmail, phoneNumbers.Count);

            _logger.LogDebug("Creating customer message payload...");
            // Create message payload from generated data
            var message = new
            {
                instruction = "create",
                entityType = "Customer",
                payload = new
                {
                    name = customerName,
                    email = customerEmail,
                    phoneNumbers = phoneNumbers
                }
            };
            _logger.LogDebug("Message payload created successfully");

            _logger.LogInformation("Publishing message to RabbitMQ...");
            await PublishMessageAsync("messaging_exchange", "customer.create", message);
            _logger.LogInformation("Message published to RabbitMQ: Instruction=create, EntityType=Customer, RoutingKey=customer.create");

            // Return accepted response
            var responseDto = new CustomerDto
            {
                Id = 0, // ID will be assigned by the Subscriber in the database
                Name = customerName,
                Email = customerEmail
            };

            _logger.LogInformation("=== Fake Customer Message Published Successfully ===");
            return Accepted(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== ERROR Publishing Fake Customer Message ===");
            _logger.LogError("Error details: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new customer and publishes a create message to RabbitMQ.
    /// </summary>
    /// <param name="createDto">The customer data transfer object</param>
    /// <returns>Created customer response</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerDto>> CreateCustomer([FromBody] CreateCustomerDto createDto)
    {
        _logger.LogInformation("=== POST /api/customers - Create Customer ===");
        _logger.LogInformation("Received request to create customer: {Name}, Email: {Email}", createDto.Name, createDto.Email);

        try
        {
            _logger.LogDebug("Creating customer message payload...");
            // Create message payload from DTO
            var message = new
            {
                instruction = "create",
                entityType = "Customer",
                payload = new
                {
                    name = createDto.Name,
                    email = createDto.Email,
                    phoneNumbers = createDto.PhoneNumbers
                }
            };
            _logger.LogDebug("Message payload created with {PhoneCount} phone numbers", createDto.PhoneNumbers?.Count ?? 0);

            _logger.LogInformation("Publishing message to RabbitMQ...");
            await PublishMessageAsync("messaging_exchange", "customer.create", message);
            _logger.LogInformation("Message published to RabbitMQ: Instruction=create, EntityType=Customer, RoutingKey=customer.create");

            // Return accepted response
            var responseDto = new CustomerDto
            {
                Id = 0, // ID will be assigned by the Subscriber in the database
                Name = createDto.Name,
                Email = createDto.Email
            };

            _logger.LogInformation("=== Customer Message Published Successfully ===");
            return Accepted(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== ERROR Publishing Customer Message ===");
            _logger.LogError("Error details: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing customer and publishes an update message to RabbitMQ.
    /// </summary>
    /// <param name="id">The customer ID</param>
    /// <param name="updateDto">The updated customer data transfer object</param>
    /// <returns>Updated customer response</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(long id, [FromBody] UpdateCustomerDto updateDto)
    {
        _logger.LogInformation("=== PUT /api/customers/{CustomerId} - Update Customer ===", id);
        _logger.LogInformation("Received request to update customer: ID={CustomerId}, Name={CustomerName}", id, updateDto.Name);

        try
        {
            _logger.LogDebug("Creating customer update message payload...");
            // Create message payload from DTO
            var message = new
            {
                instruction = "update",
                entityType = "Customer",
                id = id,
                payload = new
                {
                    name = updateDto.Name,
                    email = updateDto.Email
                }
            };
            _logger.LogDebug("Message payload created successfully");

            _logger.LogInformation("Publishing message to RabbitMQ...");
            await PublishMessageAsync("messaging_exchange", "customer.update", message);
            _logger.LogInformation("Message published to RabbitMQ: Instruction=update, EntityType=Customer, ID={CustomerId}, RoutingKey=customer.update", id);

            // Return accepted response
            var responseDto = new CustomerDto
            {
                Id = id,
                Name = updateDto.Name,
                Email = updateDto.Email
            };

            _logger.LogInformation("=== Customer Update Message Published Successfully ===");
            return Accepted(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== ERROR Publishing Customer Update Message ===");
            _logger.LogError("Error details: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }
}
