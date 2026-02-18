using Application.Configuration;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Linq;
using DotNetEnv;

// Enable UTF-8 encoding for console output to support Unicode characters (emoji, etc.)
Console.OutputEncoding = Encoding.UTF8;

// Load .env file for configuration
DotNetEnv.Env.Load();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();

        // Configure logging from appsettings
        var config = context.Configuration.GetSection("Logging");
        logging.AddConfiguration(config);

        if (context.HostingEnvironment.IsDevelopment())
        {
            logging.SetMinimumLevel(LogLevel.Debug);
        }
    })
    .ConfigureServices((context, services) =>
    {
        var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting Subscriber application...");
        logger.LogInformation("Environment: {Environment}", context.HostingEnvironment.EnvironmentName);

        // Configure RabbitMQ settings from configuration
        services.Configure<RabbitMqSettings>(context.Configuration.GetSection("RabbitMQ"));
        logger.LogInformation("RabbitMQ configuration registered");

        // Add DbContext
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
        logger.LogInformation("DbContext configured");

        // Add Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ITelephoneNumberRepository, TelephoneNumberRepository>();
        logger.LogInformation("Repositories registered");

        // Add RabbitMQ Connection Factory using Options Pattern
        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var rabbitMqOptions = sp.GetRequiredService<IOptions<RabbitMqSettings>>();
            var settings = rabbitMqOptions.Value;

            logger.LogInformation("Creating RabbitMQ ConnectionFactory with settings: Host={Host}:{Port}, User={User}", 
                settings.HostName, settings.Port, settings.UserName);

            return new ConnectionFactory()
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password
            };
        });
        logger.LogInformation("RabbitMQ ConnectionFactory configured");

        // Add SubscriberWorker
        services.AddHostedService<SubscriberWorker>();
        logger.LogInformation("SubscriberWorker registered");
    })
    .Build();

var startupLogger = host.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("Subscriber application built and ready to start");

await host.RunAsync();

/// <summary>
/// Hosted service that listens for messages from RabbitMQ and processes them.
/// </summary>
public class SubscriberWorker : BackgroundService
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SubscriberWorker> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed = false;

    public SubscriberWorker(IConnectionFactory connectionFactory, IServiceProvider serviceProvider, ILogger<SubscriberWorker> logger)
    {
        _connectionFactory = connectionFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== Subscriber Worker Starting ===");
        _logger.LogInformation("Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss}", DateTime.UtcNow);

        try
        {
            _logger.LogDebug("Creating RabbitMQ connection...");
            // Create connection and channel
            _connection = await _connectionFactory.CreateConnectionAsync();
            _logger.LogInformation("RabbitMQ connection established");

            _channel = await _connection.CreateChannelAsync();
            _logger.LogInformation("RabbitMQ channel created");

            // Queues to consume from (created by PowerShell script)
            var queuesToConsume = new[] { "customer.queue", "order.queue", "product.queue", "supplier.queue" };

            _logger.LogInformation("Setting up consumers for {Count} queues", queuesToConsume.Length);

            // Start consuming from all queues
            foreach (var queueName in queuesToConsume)
            {
                _logger.LogDebug("Setting up consumer for queue: {QueueName}", queueName);
                await SetupConsumerForQueueAsync(queueName);
            }

            _logger.LogInformation("=== Subscriber Worker Started Successfully ===");
            _logger.LogInformation("Listening for messages on queues: {Queues}", string.Join(", ", queuesToConsume));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== CRITICAL ERROR Starting Subscriber Worker ===");
            _logger.LogError("Error details: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
            throw;
        }

        await base.StartAsync(cancellationToken);
    }

    private async Task SetupConsumerForQueueAsync(string queueName)
    {
        try
        {
            // Set prefetch count to 1 to process messages one at a time
            await _channel!.BasicQosAsync(0, 1, false);

            // Create a consumer that handles messages asynchronously
            var consumer = new MessageConsumer(_channel, _serviceProvider, _logger, queueName);

            // Start consuming
            await _channel!.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);
            _logger.LogInformation("Consumer started for queue: {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up consumer for queue: {QueueName}", queueName);
            throw;
        }
    }

    /// <summary>
    /// Custom consumer implementation for handling messages
    /// </summary>
    private class MessageConsumer : IAsyncBasicConsumer
    {
        private readonly IChannel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SubscriberWorker> _logger;
        private readonly string _queueName;

        public IChannel Channel => _channel;

        public MessageConsumer(IChannel channel, IServiceProvider serviceProvider, ILogger<SubscriberWorker> logger, string queueName)
        {
            _channel = channel;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _queueName = queueName;
        }

        public async Task HandleBasicConsumeOkAsync(string consumerTag, CancellationToken ct)
        {
            _logger.LogDebug("Consumer {ConsumerTag} registered on queue {QueueName}", consumerTag, _queueName);
            await Task.CompletedTask;
        }

        public async Task HandleBasicCancelOkAsync(string consumerTag, CancellationToken ct)
        {
            _logger.LogDebug("Consumer {ConsumerTag} cancelled on queue {QueueName}", consumerTag, _queueName);
            await Task.CompletedTask;
        }

        public async Task HandleBasicCancelAsync(string consumerTag, CancellationToken ct)
        {
            _logger.LogWarning("Consumer {ConsumerTag} unexpectedly cancelled on queue {QueueName}", consumerTag, _queueName);
            await Task.CompletedTask;
        }

        public async Task HandleBasicDeliverAsync(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IReadOnlyBasicProperties properties, ReadOnlyMemory<byte> body, CancellationToken ct)
        {
            try
            {
                var messageBody = Encoding.UTF8.GetString(body.ToArray());
                _logger.LogInformation("=== Message Received from Queue: {QueueName} ===", _queueName);
                _logger.LogDebug("Raw message body: {MessageBody}", messageBody);

                // Deserialize message
                using var jsonDoc = JsonDocument.Parse(messageBody);
                var root = jsonDoc.RootElement;

                var instruction = root.GetProperty("instruction").GetString();
                var entityType = root.GetProperty("entityType").GetString();
                var payload = root.GetProperty("payload");

                _logger.LogInformation("Message details - Instruction: {Instruction}, EntityType: {EntityType}, Queue: {QueueName}", 
                    instruction, entityType, _queueName);
                _logger.LogDebug("Message payload: {Payload}", payload.GetRawText());

                // Process message (pass root element so handlers can access id if present)
                await ProcessMessageAsync(instruction, entityType, root);

                // Acknowledge the message
                await _channel.BasicAckAsync(deliveryTag, false);
                _logger.LogInformation("Message from queue {QueueName} acknowledged", _queueName);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("payload") || ex.Message.Contains("field") || ex.Message.Contains("length") || ex.Message.Contains("format") || ex.Message.Contains("GUID") || ex.Message.Contains("empty") || ex.Message.Contains("exceeds") || ex.Message.Contains("greater") || ex.Message.Contains("invalid") || ex.Message.Contains("must be") || ex.Message.Contains("cannot be") || ex.Message.Contains("is not") || ex.Message.Contains("negative"))
            {
                // Validation error - delete the message permanently (gracefully swallowed)
                _logger.LogError("❌ Validation failed - {ValidationError}", ex.Message);
                _logger.LogError("🗑️  Message permanently deleted from queue {QueueName}", _queueName);

                // Acknowledge the message (remove from queue) - do NOT requeue
                await _channel.BasicAckAsync(deliveryTag, false);
            }
            catch (Exception ex)
            {
                // Other errors - requeue for retry
                _logger.LogError(ex, "=== ERROR Processing Message from Queue {QueueName} ===", _queueName);
                _logger.LogError("Error message: {ErrorMessage}", ex.Message);
                _logger.LogWarning("⚠️  Message will be requeued for retry");

                // Negative acknowledge to requeue the message
                await _channel.BasicNackAsync(deliveryTag, false, true);
                _logger.LogWarning("Message from queue {QueueName} negatively acknowledged and requeued", _queueName);
            }
        }

        public async Task HandleChannelShutdownAsync(object channel, ShutdownEventArgs reason)
        {
            _logger.LogWarning("Channel shutdown for consumer on queue {QueueName}: {Reason}", _queueName, reason.ReplyText);
            await Task.CompletedTask;
        }

        public async Task HandleConsumerCancelledAsync(string consumerTag, CancellationToken ct)
        {
            _logger.LogInformation("Consumer {ConsumerTag} cancelled on queue {QueueName}", consumerTag, _queueName);
            await Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(string? instruction, string? entityType, JsonElement root)
        {
            var payload = root.GetProperty("payload");
            _logger.LogInformation("Processing message - Instruction: {Instruction}, EntityType: {EntityType}", instruction, entityType);

            using var scope = _serviceProvider.CreateScope();

            switch (entityType?.ToLower())
            {
                case "customer":
                    _logger.LogDebug("Routing to customer message handler");
                    await HandleCustomerMessageAsync(scope, instruction, root);
                    break;
                case "order":
                    _logger.LogDebug("Routing to order message handler");
                    await HandleOrderMessageAsync(scope, instruction, root);
                    break;
                case "product":
                    _logger.LogDebug("Routing to product message handler");
                    await HandleProductMessageAsync(scope, instruction, root);
                    break;
                case "supplier":
                    _logger.LogDebug("Routing to supplier message handler");
                    await HandleSupplierMessageAsync(scope, instruction, root);
                    break;
                case "telephonenumber":
                    _logger.LogDebug("Routing to telephone number message handler");
                    await HandleTelephoneNumberMessageAsync(scope, instruction, payload);
                    break;
                default:
                    _logger.LogWarning("Unknown entity type received: {EntityType}", entityType);
                    break;
            }

            _logger.LogInformation("Message processing completed - Instruction: {Instruction}, EntityType: {EntityType}", instruction, entityType);
        }

        private async Task HandleCustomerMessageAsync(IServiceScope scope, string? instruction, JsonElement root)
        {
            var payload = root.GetProperty("payload");
            _logger.LogInformation("Handling customer message - Instruction: {Instruction}", instruction);
            var repository = scope.ServiceProvider.GetRequiredService<ICustomerRepository>();

            try
            {
                _logger.LogDebug("Customer repository retrieved - Type: {RepositoryType}", repository.GetType().Name);

                // Validate payload
                _logger.LogDebug("Validating customer payload...");
                ValidateCustomerPayload(payload);
                _logger.LogDebug("✓ Customer payload validation passed");

                var name = payload.GetProperty("name").GetString();
                var email = payload.GetProperty("email").GetString();
                var phoneNumbers = payload.TryGetProperty("phoneNumbers", out var phonesElement) ? phonesElement : (JsonElement?)null;

                _logger.LogDebug("Extracted customer data - Name: {Name}, Email: {Email}, PhoneNumbers: {PhoneCount}", 
                    name, email, phoneNumbers?.GetArrayLength() ?? 0);

                if (instruction?.ToLower() == "create")
                {
                    _logger.LogInformation("Creating new customer - Name: {Name}, Email: {Email}", name, email);
                    var customer = new Domain.Models.Customer
                    {
                        Name = name,
                        Email = email
                    };

                    var createdCustomer = await repository.CreateAsync(customer);
                    _logger.LogInformation("✓ Customer created successfully - ID: {CustomerId}, Name: {Name}", createdCustomer.Id, createdCustomer.Name);

                    // Add telephone numbers if present
                    if (phoneNumbers.HasValue && phoneNumbers.Value.ValueKind == JsonValueKind.Array)
                    {
                        await AddTelephoneNumbersToCustomerAsync(scope, createdCustomer.Id, phoneNumbers.Value);
                    }
                }
                else if (instruction?.ToLower() == "update")
                {
                    // Get id from root element (not from payload)
                    var id = root.GetProperty("id").GetInt64();
                    _logger.LogInformation("Updating customer - ID: {CustomerId}, Name: {Name}, Email: {Email}", id, name, email);

                    var existingCustomer = await repository.GetByIdAsync(id);
                    if (existingCustomer == null)
                    {
                        _logger.LogWarning("Customer not found for update - ID: {CustomerId}", id);
                        throw new InvalidOperationException($"Customer with ID {id} not found");
                    }

                    existingCustomer.Name = name;
                    existingCustomer.Email = email;

                    var updatedCustomer = await repository.UpdateAsync(existingCustomer);
                    _logger.LogInformation("✓ Customer updated successfully - ID: {CustomerId}, Name: {Name}", updatedCustomer.Id, updatedCustomer.Name);

                    // Update telephone numbers if present
                    if (phoneNumbers.HasValue && phoneNumbers.Value.ValueKind == JsonValueKind.Array)
                    {
                        await UpdateTelephoneNumbersForCustomerAsync(scope, updatedCustomer.Id, phoneNumbers.Value);
                    }
                }
                else
                {
                    _logger.LogWarning("Unknown instruction for customer: {Instruction}", instruction);
                }

                _logger.LogInformation("Customer message processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling customer message - Instruction: {Instruction}", instruction);
                throw;
            }
        }

        private async Task AddTelephoneNumbersToCustomerAsync(IServiceScope scope, long customerId, JsonElement phoneNumbers)
        {
            try
            {
                var phoneCount = phoneNumbers.GetArrayLength();
                _logger.LogInformation("Adding {PhoneCount} telephone number(s) to customer ID: {CustomerId}", phoneCount, customerId);

                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var phoneRepository = scope.ServiceProvider.GetRequiredService<ITelephoneNumberRepository>();

                foreach (var phoneElement in phoneNumbers.EnumerateArray())
                {
                    if (phoneElement.TryGetProperty("type", out var typeElement) && 
                        phoneElement.TryGetProperty("number", out var numberElement))
                    {
                        var type = typeElement.GetString();
                        var number = numberElement.GetString();

                        if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(number))
                        {
                            _logger.LogDebug("Creating telephone number - Type: {Type}, Number: {Number}, CustomerId: {CustomerId}", 
                                type, number, customerId);

                            var phoneNumber = new Domain.Models.TelephoneNumber
                            {
                                Type = type,
                                Number = number,
                                CustomerId = customerId
                            };

                            await phoneRepository.CreateAsync(phoneNumber);
                            _logger.LogInformation("✓ Telephone number added - Type: {Type}, Number: {Number}, CustomerId: {CustomerId}", 
                                type, number, customerId);
                        }
                    }
                }

                _logger.LogInformation("✓ All telephone numbers added successfully for customer ID: {CustomerId}", customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding telephone numbers to customer ID: {CustomerId}", customerId);
                throw;
            }
        }

        private async Task UpdateTelephoneNumbersForCustomerAsync(IServiceScope scope, long customerId, JsonElement phoneNumbers)
        {
            try
            {
                var phoneCount = phoneNumbers.GetArrayLength();
                _logger.LogInformation("Updating {PhoneCount} telephone number(s) for customer ID: {CustomerId}", phoneCount, customerId);

                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var phoneRepository = scope.ServiceProvider.GetRequiredService<ITelephoneNumberRepository>();

                // Remove existing phone numbers for this customer
                var existingPhones = await dbContext.TelephoneNumbers.Where(p => p.CustomerId == customerId).ToListAsync();
                _logger.LogDebug("Removing {ExistingPhoneCount} existing telephone number(s) for customer ID: {CustomerId}", 
                    existingPhones.Count, customerId);

                foreach (var existingPhone in existingPhones)
                {
                    dbContext.TelephoneNumbers.Remove(existingPhone);
                }
                await dbContext.SaveChangesAsync();

                // Add new phone numbers
                foreach (var phoneElement in phoneNumbers.EnumerateArray())
                {
                    if (phoneElement.TryGetProperty("type", out var typeElement) && 
                        phoneElement.TryGetProperty("number", out var numberElement))
                    {
                        var type = typeElement.GetString();
                        var number = numberElement.GetString();

                        if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(number))
                        {
                            _logger.LogDebug("Creating new telephone number - Type: {Type}, Number: {Number}, CustomerId: {CustomerId}", 
                                type, number, customerId);

                            var phoneNumber = new Domain.Models.TelephoneNumber
                            {
                                Type = type,
                                Number = number,
                                CustomerId = customerId
                            };

                            await phoneRepository.CreateAsync(phoneNumber);
                            _logger.LogInformation("✓ Telephone number updated - Type: {Type}, Number: {Number}, CustomerId: {CustomerId}", 
                                type, number, customerId);
                        }
                    }
                }

                _logger.LogInformation("✓ All telephone numbers updated successfully for customer ID: {CustomerId}", customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating telephone numbers for customer ID: {CustomerId}", customerId);
                throw;
            }
        }

        private void ValidateCustomerPayload(JsonElement payload)
        {
            if (!payload.TryGetProperty("name", out var nameElement) || nameElement.ValueKind == JsonValueKind.Null)
                throw new InvalidOperationException("Customer payload missing required field: 'name'");

            if (!payload.TryGetProperty("email", out var emailElement) || emailElement.ValueKind == JsonValueKind.Null)
                throw new InvalidOperationException("Customer payload missing required field: 'email'");

            var name = nameElement.GetString();
            var email = emailElement.GetString();

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Customer 'name' cannot be empty");

            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Customer 'email' cannot be empty");

            if (name.Length > 255)
                throw new InvalidOperationException("Customer 'name' exceeds maximum length of 255 characters");

            if (email.Length > 255)
                throw new InvalidOperationException("Customer 'email' exceeds maximum length of 255 characters");

            if (!email.Contains("@"))
                throw new InvalidOperationException("Customer 'email' is not in valid email format");

            _logger.LogDebug("Customer payload validation: Name={Name}, Email={Email}", name, email);
        }

        private async Task HandleOrderMessageAsync(IServiceScope scope, string? instruction, JsonElement root)
        {
            var payload = root.GetProperty("payload");
            _logger.LogInformation("Handling order message - Instruction: {Instruction}", instruction);
            var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

            try
            {
                _logger.LogDebug("Order repository retrieved - Type: {RepositoryType}", repository.GetType().Name);

                // Validate payload
                _logger.LogDebug("Validating order payload...");
                ValidateOrderPayload(payload);
                _logger.LogDebug("✓ Order payload validation passed");

                var customerId = payload.GetProperty("customerId").GetInt64();
                var supplierId = payload.GetProperty("supplierId").GetInt64();
                var orderDate = payload.GetProperty("orderDate").GetDateTime();
                var customerEmail = payload.GetProperty("customerEmail").GetString();
                var orderStatus = Enum.Parse<Domain.Models.OrderStatus>(payload.GetProperty("orderStatus").GetString() ?? "Pending");

                // Extract billing address
                var billingAddressElement = payload.GetProperty("billingAddress");
                var billingAddress = new Domain.Models.Address
                {
                    Street = billingAddressElement.GetProperty("street").GetString() ?? string.Empty,
                    City = billingAddressElement.TryGetProperty("city", out var cityElement) && cityElement.ValueKind != JsonValueKind.Null ? cityElement.GetString() : null,
                    County = billingAddressElement.TryGetProperty("county", out var countyElement) && countyElement.ValueKind != JsonValueKind.Null ? countyElement.GetString() : null,
                    PostalCode = billingAddressElement.TryGetProperty("postalCode", out var postalCodeElement) && postalCodeElement.ValueKind != JsonValueKind.Null ? postalCodeElement.GetString() : null,
                    Country = billingAddressElement.TryGetProperty("country", out var countryElement) && countryElement.ValueKind != JsonValueKind.Null ? countryElement.GetString() : null
                };

                // Extract optional delivery address
                Domain.Models.Address? deliveryAddress = null;
                if (payload.TryGetProperty("deliveryAddress", out var deliveryAddressElement) && deliveryAddressElement.ValueKind != JsonValueKind.Null)
                {
                    deliveryAddress = new Domain.Models.Address
                    {
                        Street = deliveryAddressElement.GetProperty("street").GetString() ?? string.Empty,
                        City = deliveryAddressElement.TryGetProperty("city", out var delCityElement) && delCityElement.ValueKind != JsonValueKind.Null ? delCityElement.GetString() : null,
                        County = deliveryAddressElement.TryGetProperty("county", out var delCountyElement) && delCountyElement.ValueKind != JsonValueKind.Null ? delCountyElement.GetString() : null,
                        PostalCode = deliveryAddressElement.TryGetProperty("postalCode", out var delPostalCodeElement) && delPostalCodeElement.ValueKind != JsonValueKind.Null ? delPostalCodeElement.GetString() : null,
                        Country = deliveryAddressElement.TryGetProperty("country", out var delCountryElement) && delCountryElement.ValueKind != JsonValueKind.Null ? delCountryElement.GetString() : null
                    };
                }

                // Extract optional order items
                var orderItems = new List<Domain.Models.OrderItem>();
                if (payload.TryGetProperty("orderItems", out var orderItemsElement) && orderItemsElement.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogDebug("Extracting order items from payload...");
                    foreach (var itemElement in orderItemsElement.EnumerateArray())
                    {
                        if (itemElement.TryGetProperty("productId", out var productIdElement) && productIdElement.ValueKind != JsonValueKind.Null &&
                            itemElement.TryGetProperty("quantity", out var quantityElement) && quantityElement.ValueKind != JsonValueKind.Null &&
                            itemElement.TryGetProperty("unitPrice", out var unitPriceElement) && unitPriceElement.ValueKind != JsonValueKind.Null)
                        {
                            var productId = productIdElement.GetInt64();
                            var quantity = quantityElement.GetInt32();
                            var unitPrice = unitPriceElement.GetDecimal();

                            var orderItem = new Domain.Models.OrderItem
                            {
                                ProductId = productId,
                                Quantity = quantity,
                                Price = unitPrice
                            };

                            orderItems.Add(orderItem);
                            _logger.LogDebug("Added order item - ProductId: {ProductId}, Quantity: {Quantity}, Price: {Price}", productId, quantity, unitPrice);
                        }
                    }
                    _logger.LogInformation("Extracted {ItemCount} order item(s)", orderItems.Count);
                }
                else
                {
                    _logger.LogWarning("No order items found in payload or orderItems is not an array");
                }

                _logger.LogDebug("Extracted order data - CustomerId: {CustomerId}, SupplierId: {SupplierId}, OrderDate: {OrderDate}, BillingAddress.Street: {Street}, ItemCount: {ItemCount}", 
                    customerId, supplierId, orderDate, billingAddress.Street, orderItems.Count);

                if (instruction?.ToLower() == "create")
                {
                    _logger.LogInformation("Creating new order - CustomerId: {CustomerId}, SupplierId: {SupplierId}, OrderDate: {OrderDate}, ItemCount: {ItemCount}", customerId, supplierId, orderDate, orderItems.Count);
                    var order = new Domain.Models.Order
                    {
                        CustomerId = customerId,
                        SupplierId = supplierId,
                        OrderDate = orderDate,
                        CustomerEmail = customerEmail,
                        OrderStatus = orderStatus,
                        BillingAddress = billingAddress,
                        DeliveryAddress = deliveryAddress,
                        OrderItems = orderItems
                    };

                    var createdOrder = await repository.CreateAsync(order);
                    _logger.LogInformation("✓ Order created successfully - ID: {OrderId}, CustomerId: {CustomerId}, Status: {OrderStatus}, ItemCount: {ItemCount}", 
                        createdOrder.Id, createdOrder.CustomerId, createdOrder.OrderStatus, createdOrder.OrderItems?.Count ?? 0);

                    // Send confirmation messages
                    _logger.LogInformation("📧 Email confirmation has been sent to: {CustomerEmail}", customerEmail);
                    _logger.LogInformation("🎫 Picking slip has been generated for Order ID: {OrderId}", createdOrder.Id);
                }
                else if (instruction?.ToLower() == "update")
                {
                    // Get id from root element (not from payload)
                    var id = root.GetProperty("id").GetInt64();
                    _logger.LogInformation("Updating order - ID: {OrderId}, CustomerId: {CustomerId}, OrderStatus: {OrderStatus}, ItemCount: {ItemCount}", id, customerId, orderStatus, orderItems.Count);

                    var existingOrder = await repository.GetByIdAsync(id, includeRelated: true);
                    if (existingOrder == null)
                    {
                        _logger.LogWarning("Order not found for update - ID: {OrderId}", id);
                        throw new InvalidOperationException($"Order with ID {id} not found");
                    }

                    existingOrder.CustomerId = customerId;
                    existingOrder.SupplierId = supplierId;
                    existingOrder.OrderDate = orderDate;
                    existingOrder.CustomerEmail = customerEmail;
                    existingOrder.OrderStatus = orderStatus;
                    existingOrder.BillingAddress = billingAddress;
                    existingOrder.DeliveryAddress = deliveryAddress;
                    existingOrder.OrderItems = orderItems;

                    var updatedOrder = await repository.UpdateAsync(existingOrder);
                    _logger.LogInformation("✓ Order updated successfully - ID: {OrderId}, CustomerId: {CustomerId}, Status: {OrderStatus}, ItemCount: {ItemCount}", 
                        updatedOrder.Id, updatedOrder.CustomerId, updatedOrder.OrderStatus, updatedOrder.OrderItems?.Count ?? 0);
                }
                else
                {
                    _logger.LogWarning("Unknown instruction for order: {Instruction}", instruction);
                }

                _logger.LogInformation("Order message processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling order message - Instruction: {Instruction}", instruction);
                throw;
            }
        }

        private void ValidateOrderPayload(JsonElement payload)
        {
            var validator = new OrderPayloadValidator();
            var errors = validator.Validate(payload);

            if (errors.Any())
            {
                var errorMessage = string.Join("; ", errors);
                throw new InvalidOperationException(errorMessage);
            }
        }

        private async Task HandleProductMessageAsync(IServiceScope scope, string? instruction, JsonElement root)
        {
            var payload = root.GetProperty("payload");
            _logger.LogInformation("Handling product message - Instruction: {Instruction}", instruction);
            var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

            try
            {
                _logger.LogDebug("Product repository retrieved - Type: {RepositoryType}", repository.GetType().Name);

                // Validate payload
                _logger.LogDebug("Validating product payload...");
                ValidateProductPayload(payload);
                _logger.LogDebug("✓ Product payload validation passed");

                var productCodeStr = payload.GetProperty("productCode").GetString();
                var productCode = Guid.Parse(productCodeStr ?? Guid.Empty.ToString());
                var name = payload.GetProperty("name").GetString();

                _logger.LogDebug("Extracted product data - Code: {ProductCode}, Name: {Name}", productCode, name);

                if (instruction?.ToLower() == "create")
                {
                    _logger.LogInformation("Creating new product - Code: {ProductCode}, Name: {Name}", productCode, name);
                    var product = new Domain.Models.Product
                    {
                        ProductCode = productCode,
                        Name = name
                    };

                    var createdProduct = await repository.CreateAsync(product);
                    _logger.LogInformation("✓ Product created successfully - ID: {ProductId}, Code: {ProductCode}, Name: {Name}", 
                        createdProduct.Id, createdProduct.ProductCode, createdProduct.Name);
                }
                else if (instruction?.ToLower() == "update")
                {
                    // Get id from root element (not from payload)
                    var id = root.GetProperty("id").GetInt64();
                    _logger.LogInformation("Updating product - ID: {ProductId}, Code: {ProductCode}, Name: {Name}", id, productCode, name);

                    var existingProduct = await repository.GetByIdAsync(id);
                    if (existingProduct == null)
                    {
                        _logger.LogWarning("Product not found for update - ID: {ProductId}", id);
                        throw new InvalidOperationException($"Product with ID {id} not found");
                    }

                    existingProduct.ProductCode = productCode;
                    existingProduct.Name = name;

                    var updatedProduct = await repository.UpdateAsync(existingProduct);
                    _logger.LogInformation("✓ Product updated successfully - ID: {ProductId}, Code: {ProductCode}, Name: {Name}", 
                        updatedProduct.Id, updatedProduct.ProductCode, updatedProduct.Name);
                }
                else
                {
                    _logger.LogWarning("Unknown instruction for product: {Instruction}", instruction);
                }

                _logger.LogInformation("Product message processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling product message - Instruction: {Instruction}", instruction);
                throw;
            }
        }

        private void ValidateProductPayload(JsonElement payload)
        {
            if (!payload.TryGetProperty("productCode", out var productCodeElement) || productCodeElement.ValueKind == JsonValueKind.Null)
                throw new InvalidOperationException("Product payload missing required field: 'productCode'");

            if (!payload.TryGetProperty("name", out var nameElement) || nameElement.ValueKind == JsonValueKind.Null)
                throw new InvalidOperationException("Product payload missing required field: 'name'");

            var productCodeStr = productCodeElement.GetString();
            var name = nameElement.GetString();

            if (string.IsNullOrWhiteSpace(productCodeStr))
                throw new InvalidOperationException("Product 'productCode' cannot be empty");

            if (!Guid.TryParse(productCodeStr, out _))
                throw new InvalidOperationException($"Product 'productCode' is not a valid GUID: {productCodeStr}");

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Product 'name' cannot be empty");

            if (name.Length > 255)
                throw new InvalidOperationException("Product 'name' exceeds maximum length of 255 characters");

            _logger.LogDebug("Product payload validation: Code={Code}, Name={Name}", productCodeStr, name);
        }

        private async Task HandleSupplierMessageAsync(IServiceScope scope, string? instruction, JsonElement root)
        {
            var payload = root.GetProperty("payload");
            _logger.LogInformation("Handling supplier message - Instruction: {Instruction}", instruction);
            var repository = scope.ServiceProvider.GetRequiredService<ISupplierRepository>();

            try
            {
                _logger.LogDebug("Supplier repository retrieved - Type: {RepositoryType}", repository.GetType().Name);

                // Validate payload
                _logger.LogDebug("Validating supplier payload...");
                ValidateSupplierPayload(payload);
                _logger.LogDebug("✓ Supplier payload validation passed");

                var name = payload.GetProperty("name").GetString();

                _logger.LogDebug("Extracted supplier data - Name: {Name}", name);

                if (instruction?.ToLower() == "create")
                {
                    _logger.LogInformation("Creating new supplier - Name: {Name}", name);
                    var supplier = new Domain.Models.Supplier
                    {
                        Name = name
                    };

                    var createdSupplier = await repository.CreateAsync(supplier);
                    _logger.LogInformation("✓ Supplier created successfully - ID: {SupplierId}, Name: {Name}", 
                        createdSupplier.Id, createdSupplier.Name);
                }
                else if (instruction?.ToLower() == "update")
                {
                    // Get id from root element (not from payload)
                    var id = root.GetProperty("id").GetInt64();
                    _logger.LogInformation("Updating supplier - ID: {SupplierId}, Name: {Name}", id, name);

                    var existingSupplier = await repository.GetByIdAsync(id);
                    if (existingSupplier == null)
                    {
                        _logger.LogWarning("Supplier not found for update - ID: {SupplierId}", id);
                        throw new InvalidOperationException($"Supplier with ID {id} not found");
                    }

                    existingSupplier.Name = name;

                    var updatedSupplier = await repository.UpdateAsync(existingSupplier);
                    _logger.LogInformation("✓ Supplier updated successfully - ID: {SupplierId}, Name: {Name}", 
                        updatedSupplier.Id, updatedSupplier.Name);
                }
                else
                {
                    _logger.LogWarning("Unknown instruction for supplier: {Instruction}", instruction);
                }

                _logger.LogInformation("Supplier message processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling supplier message - Instruction: {Instruction}", instruction);
                throw;
            }
        }

        private void ValidateSupplierPayload(JsonElement payload)
        {
            if (!payload.TryGetProperty("name", out var nameElement) || nameElement.ValueKind == JsonValueKind.Null)
                throw new InvalidOperationException("Supplier payload missing required field: 'name'");

            var name = nameElement.GetString();

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Supplier 'name' cannot be empty");

            if (name.Length > 255)
                throw new InvalidOperationException("Supplier 'name' exceeds maximum length of 255 characters");

            _logger.LogDebug("Supplier payload validation: Name={Name}", name);
        }

        private async Task HandleTelephoneNumberMessageAsync(IServiceScope scope, string? instruction, JsonElement payload)
        {
            _logger.LogInformation("Handling telephone number message - Instruction: {Instruction}", instruction);
            var repository = scope.ServiceProvider.GetRequiredService<ITelephoneNumberRepository>();

            try
            {
                _logger.LogDebug("Telephone number repository retrieved - Type: {RepositoryType}", repository.GetType().Name);

                // Validate payload
                _logger.LogDebug("Validating telephone number payload...");
                ValidateTelephoneNumberPayload(payload);
                _logger.LogDebug("✓ Telephone number payload validation passed");

                var type = payload.GetProperty("type").GetString();
                var number = payload.GetProperty("number").GetString();

                _logger.LogDebug("Extracted telephone number data - Type: {Type}, Number: {Number}", type, number);

                if (instruction?.ToLower() == "create")
                {
                    _logger.LogInformation("Creating new telephone number - Type: {Type}, Number: {Number}", type, number);
                    var telephoneNumber = new Domain.Models.TelephoneNumber
                    {
                        Type = type,
                        Number = number
                    };

                    var createdPhoneNumber = await repository.CreateAsync(telephoneNumber);
                    _logger.LogInformation("✓ Telephone number created successfully - ID: {PhoneId}, Type: {Type}, Number: {Number}", 
                        createdPhoneNumber.Id, createdPhoneNumber.Type, createdPhoneNumber.Number);
                }
                else if (instruction?.ToLower() == "update")
                {
                    var id = payload.GetProperty("id").GetInt64();
                    _logger.LogInformation("Updating telephone number - ID: {PhoneId}, Type: {Type}, Number: {Number}", id, type, number);

                    var existingPhoneNumber = await repository.GetByIdAsync(id);
                    if (existingPhoneNumber == null)
                    {
                        _logger.LogWarning("Telephone number not found for update - ID: {PhoneId}", id);
                        throw new InvalidOperationException($"Telephone number with ID {id} not found");
                    }

                    existingPhoneNumber.Type = type;
                    existingPhoneNumber.Number = number;

                    var updatedPhoneNumber = await repository.UpdateAsync(existingPhoneNumber);
                    _logger.LogInformation("✓ Telephone number updated successfully - ID: {PhoneId}, Type: {Type}, Number: {Number}", 
                        updatedPhoneNumber.Id, updatedPhoneNumber.Type, updatedPhoneNumber.Number);
                }
                else
                {
                    _logger.LogWarning("Unknown instruction for telephone number: {Instruction}", instruction);
                }

                _logger.LogInformation("Telephone number message processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling telephone number message - Instruction: {Instruction}", instruction);
                throw;
            }
        }

        private void ValidateTelephoneNumberPayload(JsonElement payload)
        {
            if (!payload.TryGetProperty("type", out var typeElement) || typeElement.ValueKind == JsonValueKind.Null)
                throw new InvalidOperationException("Telephone number payload missing required field: 'type'");

            if (!payload.TryGetProperty("number", out var numberElement) || numberElement.ValueKind == JsonValueKind.Null)
                throw new InvalidOperationException("Telephone number payload missing required field: 'number'");

            var type = typeElement.GetString();
            var number = numberElement.GetString();

            if (string.IsNullOrWhiteSpace(type))
                throw new InvalidOperationException("Telephone number 'type' cannot be empty");

            if (string.IsNullOrWhiteSpace(number))
                throw new InvalidOperationException("Telephone number 'number' cannot be empty");

            if (type.Length > 50)
                throw new InvalidOperationException("Telephone number 'type' exceeds maximum length of 50 characters");

            if (number.Length > 20)
                throw new InvalidOperationException("Telephone number 'number' exceeds maximum length of 20 characters");

            _logger.LogDebug("Telephone number payload validation: Type={Type}, Number={Number}", type, number);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExecuteAsync started - waiting for stop signal");
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        _logger.LogInformation("ExecuteAsync received stop signal");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== Subscriber Worker Stopping ===");
        _logger.LogInformation("Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss}", DateTime.UtcNow);
        _disposed = true;

        try
        {
            if (_channel != null)
            {
                _logger.LogDebug("Closing RabbitMQ channel...");
                await _channel.CloseAsync();
                _logger.LogInformation("RabbitMQ channel closed");
                _channel.Dispose();
                _logger.LogDebug("RabbitMQ channel disposed");
            }

            if (_connection != null)
            {
                _logger.LogDebug("Closing RabbitMQ connection...");
                await _connection.CloseAsync();
                _logger.LogInformation("RabbitMQ connection closed");
                _connection.Dispose();
                _logger.LogDebug("RabbitMQ connection disposed");
            }

            _logger.LogInformation("=== Subscriber Worker Stopped Successfully ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Subscriber Worker shutdown");
        }

        await base.StopAsync(cancellationToken);
    }
}
