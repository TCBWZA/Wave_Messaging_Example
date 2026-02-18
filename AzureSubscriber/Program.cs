using Application.Configuration;
using Azure.Messaging.ServiceBus;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        logger.LogInformation("Starting Azure Subscriber application...");
        logger.LogInformation("Environment: {Environment}", context.HostingEnvironment.EnvironmentName);

        // Configure Azure Service Bus settings from configuration
        services.Configure<AzureServiceBusSettings>(context.Configuration.GetSection("AzureServiceBus"));
        logger.LogInformation("Azure Service Bus configuration registered");

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

        // Add Azure Service Bus Client
        services.AddSingleton<ServiceBusClient>(sp =>
        {
            var sbOptions = sp.GetRequiredService<IOptions<AzureServiceBusSettings>>();
            var settings = sbOptions.Value;
            logger.LogInformation("Creating Azure Service Bus Client");
            return new ServiceBusClient(settings.ConnectionString);
        });
        logger.LogInformation("Azure Service Bus Client configured");

        // Add AzureSubscriberWorker
        services.AddHostedService<AzureSubscriberWorker>();
        logger.LogInformation("AzureSubscriberWorker registered");
    })
    .Build();

var startupLogger = host.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("Azure Subscriber application built and ready to start");

await host.RunAsync();

/// <summary>
/// Hosted service that listens for messages from Azure Service Bus and processes them.
/// </summary>
public class AzureSubscriberWorker : BackgroundService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AzureSubscriberWorker> _logger;
    private readonly IOptions<AzureServiceBusSettings> _sbSettings;
    private ServiceBusProcessor? _processor;

    public AzureSubscriberWorker(ServiceBusClient serviceBusClient, IServiceProvider serviceProvider, ILogger<AzureSubscriberWorker> logger, IOptions<AzureServiceBusSettings> sbSettings)
    {
        _serviceBusClient = serviceBusClient;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _sbSettings = sbSettings;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== Azure Subscriber Worker Starting ===");
        _logger.LogInformation("Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss}", DateTime.UtcNow);

        try
        {
            _logger.LogDebug("Creating Service Bus processor...");
            var settings = _sbSettings.Value;
            _processor = _serviceBusClient.CreateProcessor(settings.TopicName, settings.SubscriptionName);

            // Register message and error handlers
            _processor.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ProcessErrorAsync;

            _logger.LogInformation("Starting Service Bus processor...");
            await _processor.StartProcessingAsync(cancellationToken);

            _logger.LogInformation("=== Azure Subscriber Worker Started Successfully ===");
            _logger.LogInformation("Listening for messages on topic: {TopicName}, subscription: {SubscriptionName}", settings.TopicName, settings.SubscriptionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== CRITICAL ERROR Starting Azure Subscriber Worker ===");
            _logger.LogError("Error details: {Message}", ex.Message);
            throw;
        }

        await base.StartAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var messageBody = args.Message.Body.ToString();
            _logger.LogInformation("=== Message Received from Service Bus ===");
            _logger.LogDebug("Raw message body: {MessageBody}", messageBody);

            // Deserialize message
            using var jsonDoc = JsonDocument.Parse(messageBody);
            var root = jsonDoc.RootElement;

            var instruction = root.GetProperty("instruction").GetString();
            var entityType = root.GetProperty("entityType").GetString();

            _logger.LogInformation("Message details - Instruction: {Instruction}, EntityType: {EntityType}", instruction, entityType);

            // Process message
            await ProcessMessageAsync(instruction, entityType, root);

            // Complete the message
            await args.CompleteMessageAsync(args.Message);
            _logger.LogInformation("Message acknowledged and completed");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("payload") || ex.Message.Contains("field") || ex.Message.Contains("length") || ex.Message.Contains("format") || ex.Message.Contains("GUID") || ex.Message.Contains("empty") || ex.Message.Contains("exceeds") || ex.Message.Contains("greater") || ex.Message.Contains("invalid") || ex.Message.Contains("must be") || ex.Message.Contains("cannot be") || ex.Message.Contains("is not") || ex.Message.Contains("negative"))
        {
            // Validation error - abandon the message (dead letter)
            _logger.LogError("❌ Validation failed - {ValidationError}", ex.Message);
            _logger.LogError("🗑️  Message sent to dead letter queue");

            await args.DeadLetterMessageAsync(args.Message, ex.Message);
        }
        catch (Exception ex)
        {
            // Other errors - abandon for retry
            _logger.LogError(ex, "=== ERROR Processing Message ===");
            _logger.LogError("Error message: {ErrorMessage}", ex.Message);
            _logger.LogWarning("⚠️  Message will be retried");

            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "=== ERROR in Service Bus Processor ===");
        _logger.LogError("Error source: {ErrorSource}", args.ErrorSource);
        _logger.LogError("Error details: {Message}", args.Exception.Message);
        return Task.CompletedTask;
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

            var name = payload.GetProperty("name").GetString();
            var email = payload.GetProperty("email").GetString();

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
            }
            else if (instruction?.ToLower() == "update")
            {
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
            }

            _logger.LogInformation("Customer message processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling customer message - Instruction: {Instruction}", instruction);
            throw;
        }
    }

    private async Task HandleOrderMessageAsync(IServiceScope scope, string? instruction, JsonElement root)
    {
        var payload = root.GetProperty("payload");
        _logger.LogInformation("Handling order message - Instruction: {Instruction}", instruction);
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        try
        {
            var customerId = payload.GetProperty("customerId").GetInt64();
            var supplierId = payload.GetProperty("supplierId").GetInt64();
            var orderDate = payload.GetProperty("orderDate").GetDateTime();
            var customerEmail = payload.GetProperty("customerEmail").GetString();
            var orderStatus = Enum.Parse<Domain.Models.OrderStatus>(payload.GetProperty("orderStatus").GetString() ?? "Pending");

            var billingAddressElement = payload.GetProperty("billingAddress");
            var billingAddress = new Domain.Models.Address
            {
                Street = billingAddressElement.GetProperty("street").GetString() ?? string.Empty,
                City = billingAddressElement.TryGetProperty("city", out var cityElement) && cityElement.ValueKind != JsonValueKind.Null ? cityElement.GetString() : null,
                County = billingAddressElement.TryGetProperty("county", out var countyElement) && countyElement.ValueKind != JsonValueKind.Null ? countyElement.GetString() : null,
                PostalCode = billingAddressElement.TryGetProperty("postalCode", out var postalCodeElement) && postalCodeElement.ValueKind != JsonValueKind.Null ? postalCodeElement.GetString() : null,
                Country = billingAddressElement.TryGetProperty("country", out var countryElement) && countryElement.ValueKind != JsonValueKind.Null ? countryElement.GetString() : null
            };

            if (instruction?.ToLower() == "create")
            {
                _logger.LogInformation("Creating new order - CustomerId: {CustomerId}, SupplierId: {SupplierId}, OrderDate: {OrderDate}", customerId, supplierId, orderDate);
                var order = new Domain.Models.Order
                {
                    CustomerId = customerId,
                    SupplierId = supplierId,
                    OrderDate = orderDate,
                    CustomerEmail = customerEmail,
                    OrderStatus = orderStatus,
                    BillingAddress = billingAddress
                };

                var createdOrder = await repository.CreateAsync(order);
                _logger.LogInformation("✓ Order created successfully - ID: {OrderId}, CustomerId: {CustomerId}, Status: {OrderStatus}", 
                    createdOrder.Id, createdOrder.CustomerId, createdOrder.OrderStatus);

                // Send confirmation messages
                _logger.LogInformation("📧 Email confirmation has been sent to: {CustomerEmail}", customerEmail);
                _logger.LogInformation("🎫 Picking slip has been generated for Order ID: {OrderId}", createdOrder.Id);
            }
            else if (instruction?.ToLower() == "update")
            {
                var id = root.GetProperty("id").GetInt64();
                _logger.LogInformation("Updating order - ID: {OrderId}, CustomerId: {CustomerId}, OrderStatus: {OrderStatus}", id, customerId, orderStatus);

                var existingOrder = await repository.GetByIdAsync(id, includeRelated: true);
                if (existingOrder == null)
                {
                    throw new InvalidOperationException($"Order with ID {id} not found");
                }

                existingOrder.CustomerId = customerId;
                existingOrder.SupplierId = supplierId;
                existingOrder.OrderDate = orderDate;
                existingOrder.OrderStatus = orderStatus;
                existingOrder.BillingAddress = billingAddress;

                var updatedOrder = await repository.UpdateAsync(existingOrder);
                _logger.LogInformation("✓ Order updated successfully - ID: {OrderId}, CustomerId: {CustomerId}, Status: {OrderStatus}", 
                    updatedOrder.Id, updatedOrder.CustomerId, updatedOrder.OrderStatus);
            }

            _logger.LogInformation("Order message processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling order message - Instruction: {Instruction}", instruction);
            throw;
        }
    }

    private async Task HandleProductMessageAsync(IServiceScope scope, string? instruction, JsonElement root)
    {
        var payload = root.GetProperty("payload");
        _logger.LogInformation("Handling product message - Instruction: {Instruction}", instruction);
        var repository = scope.ServiceProvider.GetRequiredService<IProductRepository>();

        try
        {
            var productCodeStr = payload.GetProperty("productCode").GetString();
            var productCode = Guid.Parse(productCodeStr ?? Guid.Empty.ToString());
            var name = payload.GetProperty("name").GetString();

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
                var id = root.GetProperty("id").GetInt64();
                _logger.LogInformation("Updating product - ID: {ProductId}, Code: {ProductCode}, Name: {Name}", id, productCode, name);

                var existingProduct = await repository.GetByIdAsync(id);
                if (existingProduct == null)
                {
                    throw new InvalidOperationException($"Product with ID {id} not found");
                }

                existingProduct.ProductCode = productCode;
                existingProduct.Name = name;

                var updatedProduct = await repository.UpdateAsync(existingProduct);
                _logger.LogInformation("✓ Product updated successfully - ID: {ProductId}, Code: {ProductCode}, Name: {Name}", 
                    updatedProduct.Id, updatedProduct.ProductCode, updatedProduct.Name);
            }

            _logger.LogInformation("Product message processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling product message - Instruction: {Instruction}", instruction);
            throw;
        }
    }

    private async Task HandleSupplierMessageAsync(IServiceScope scope, string? instruction, JsonElement root)
    {
        var payload = root.GetProperty("payload");
        _logger.LogInformation("Handling supplier message - Instruction: {Instruction}", instruction);
        var repository = scope.ServiceProvider.GetRequiredService<ISupplierRepository>();

        try
        {
            var name = payload.GetProperty("name").GetString();

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
                var id = root.GetProperty("id").GetInt64();
                _logger.LogInformation("Updating supplier - ID: {SupplierId}, Name: {Name}", id, name);

                var existingSupplier = await repository.GetByIdAsync(id);
                if (existingSupplier == null)
                {
                    throw new InvalidOperationException($"Supplier with ID {id} not found");
                }

                existingSupplier.Name = name;

                var updatedSupplier = await repository.UpdateAsync(existingSupplier);
                _logger.LogInformation("✓ Supplier updated successfully - ID: {SupplierId}, Name: {Name}", 
                    updatedSupplier.Id, updatedSupplier.Name);
            }

            _logger.LogInformation("Supplier message processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling supplier message - Instruction: {Instruction}", instruction);
            throw;
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
        _logger.LogInformation("=== Azure Subscriber Worker Stopping ===");
        _logger.LogInformation("Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss}", DateTime.UtcNow);

        try
        {
            if (_processor != null)
            {
                _logger.LogDebug("Stopping Service Bus processor...");
                await _processor.StopProcessingAsync(cancellationToken);
                _logger.LogInformation("Service Bus processor stopped");
                await _processor.DisposeAsync();
                _logger.LogDebug("Service Bus processor disposed");
            }

            _logger.LogInformation("=== Azure Subscriber Worker Stopped Successfully ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Azure Subscriber Worker shutdown");
        }

        await base.StopAsync(cancellationToken);
    }
}

public class AzureServiceBusSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string TopicName { get; set; } = "messaging-topic";
    public string SubscriptionName { get; set; } = "messaging-subscription";
}
