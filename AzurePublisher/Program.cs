using Application.Configuration;
using Azure.Messaging.ServiceBus;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Text;
using System.Text.Json;
using DotNetEnv;

// Enable UTF-8 encoding for console output to support Unicode characters (emoji, etc.)
Console.OutputEncoding = Encoding.UTF8;

// Load .env file for configuration
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure logging from appsettings
var loggingConfig = builder.Configuration.GetSection("Logging");
builder.Logging.AddConfiguration(loggingConfig);

// Add services to the container

// Configure Azure Service Bus settings from configuration
builder.Services.Configure<AzureServiceBusSettings>(builder.Configuration.GetSection("AzureServiceBus"));

// Add Azure Service Bus Client
builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ServiceBusClient>>();
    var sbOptions = sp.GetRequiredService<IOptions<AzureServiceBusSettings>>();
    var settings = sbOptions.Value;

    logger.LogInformation("Creating Azure Service Bus Client with connection string");

    return new ServiceBusClient(settings.ConnectionString);
});

// Add Service Bus Sender Factory
builder.Services.AddSingleton<ServiceBusSenderFactory>();

// Add DbContext for database access
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

// Add Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Add controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Azure Publisher API",
        Version = "v1",
        Description = "RESTful API for publishing domain events to Azure Service Bus\n\n" +
                      "## Architecture\n\n" +
                      "This is a **message publisher** service that:\n" +
                      "- Accepts HTTP requests with domain entity data\n" +
                      "- Converts the data to messages\n" +
                      "- Publishes messages to Azure Service Bus Topic\n" +
                      "- Returns immediately (202 Accepted) without waiting for database persistence\n\n" +
                      "The **Subscriber** service consumes these messages from Azure Service Bus and persists them to the database.\n\n" +
                      "## How to Use\n\n" +
                      "1. Select an endpoint (Customer, Order, Product, or Supplier)\n" +
                      "2. Click **Try it out**\n" +
                      "3. Enter the request body with the entity data\n" +
                      "4. Click **Execute**\n" +
                      "5. A message will be published to Azure Service Bus (202 Accepted response)",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Azure Publisher API Support"
        }
    });

    var xmlFile = Path.Combine(AppContext.BaseDirectory, "AzurePublisher.xml");
    if (File.Exists(xmlFile))
    {
        c.IncludeXmlComments(xmlFile);
    }
});

// Add SwaggerUI middleware configuration
builder.Services.Configure<Swashbuckle.AspNetCore.SwaggerUI.SwaggerUIOptions>(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure Publisher API v1");
    options.RoutePrefix = string.Empty;
    options.DefaultModelsExpandDepth(1);
    options.DefaultModelExpandDepth(2);
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    options.DisplayOperationId();
    options.ShowExtensions();
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Azure Publisher API application...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Azure Service Bus client configured");
logger.LogInformation("DbContext configured with transient error resiliency");
logger.LogInformation("Product Repository registered");
logger.LogInformation("Controllers registered");
logger.LogInformation("Swagger/OpenAPI configured");
logger.LogInformation("CORS configured");

if (app.Environment.IsDevelopment())
{
    logger.LogInformation("Development environment detected");
}

app.UseSwagger(options =>
{
    options.RouteTemplate = "swagger/{documentName}/swagger.json";
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure Publisher API v1");
    options.RoutePrefix = string.Empty;
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    options.DefaultModelsExpandDepth(1);
    options.DefaultModelExpandDepth(2);
    options.DisplayOperationId();
    options.ShowExtensions();
    options.DocumentTitle = "Azure Publisher API - Swagger UI";
    options.EnableValidator();
    options.EnablePersistAuthorization();
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

logger.LogInformation("Azure Publisher API configured and ready to start");
logger.LogInformation("Swagger UI available at: https://localhost:7000");

app.Run();

/// <summary>
/// Factory for creating Service Bus Senders for different topics/queues
/// </summary>
public class ServiceBusSenderFactory
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IOptions<AzureServiceBusSettings> _sbSettings;
    private readonly ILogger<ServiceBusSenderFactory> _logger;
    private readonly Dictionary<string, ServiceBusSender> _senders = new();

    public ServiceBusSenderFactory(ServiceBusClient serviceBusClient, IOptions<AzureServiceBusSettings> sbSettings, ILogger<ServiceBusSenderFactory> logger)
    {
        _serviceBusClient = serviceBusClient;
        _sbSettings = sbSettings;
        _logger = logger;
    }

    public ServiceBusSender GetSender(string topicName)
    {
        if (_senders.TryGetValue(topicName, out var sender))
        {
            return sender;
        }

        sender = _serviceBusClient.CreateSender(topicName);
        _senders[topicName] = sender;
        _logger.LogInformation("Created Service Bus sender for topic: {TopicName}", topicName);
        return sender;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }
        _senders.Clear();
    }
}

public class AzureServiceBusSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string TopicName { get; set; } = "messaging-topic";
}
