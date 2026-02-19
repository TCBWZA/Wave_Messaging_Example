using Application.Configuration;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Swashbuckle.AspNetCore.SwaggerUI;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text;
using DotNetEnv;
using Swashbuckle.AspNetCore.SwaggerGen;

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

// Add more detailed logging for development
// if (builder.Environment.IsDevelopment())
// {
//    builder.Logging.SetMinimumLevel(LogLevel.Debug);
// }

// Add services to the container

// Configure RabbitMQ settings from configuration
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));

// Add RabbitMQ Connection Factory using Options Pattern
builder.Services.AddSingleton<IConnectionFactory>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<IConnectionFactory>>();
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

// Add DbContext for database access (used by OrdersController)
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

// Add FluentValidation (removed - validators are in Infrastructure)
// Note: Validators are used in the Subscriber for data validation

// Add controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Publisher API",
        Version = "v1",
        Description = "RESTful API for publishing domain events to RabbitMQ\n\n" +
                      "## Architecture\n\n" +
                      "This is a **message publisher** service that:\n" +
                      "- Accepts HTTP requests with domain entity data\n" +
                      "- Converts the data to messages\n" +
                      "- Publishes messages to RabbitMQ\n" +
                      "- Returns immediately (202 Accepted) without waiting for database persistence\n\n" +
                      "The **Subscriber** service consumes these messages from RabbitMQ and persists them to the database.\n\n" +
                      "## How to Use\n\n" +
                      "1. Select an endpoint (Customer, Order, Product, or Supplier)\n" +
                      "2. Click **Try it out**\n" +
                      "3. Enter the request body with the entity data\n" +
                      "4. Click **Execute**\n" +
                      "5. A message will be published to RabbitMQ (202 Accepted response)\n\n" +
                      "## Endpoints\n\n" +
                      "- **POST /api/customers** - Publish a create customer message\n" +
                      "- **PUT /api/customers/{id}** - Publish an update customer message\n" +
                      "- **POST /api/orders** - Publish a create order message\n" +
                      "- **PUT /api/orders/{id}** - Publish an update order message\n" +
                      "- **POST /api/products** - Publish a create product message\n" +
                      "- **PUT /api/products/{id}** - Publish an update product message\n" +
                      "- **POST /api/suppliers** - Publish a create supplier message\n" +
                      "- **PUT /api/suppliers/{id}** - Publish an update supplier message",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Duncan MacMillan"
        }
    });

    // Include XML comments if available
    var xmlFile = Path.Combine(AppContext.BaseDirectory, "Publisher.xml");
    if (File.Exists(xmlFile))
    {
        c.IncludeXmlComments(xmlFile);
    }
});

// Add SwaggerUI middleware configuration
builder.Services.Configure<Swashbuckle.AspNetCore.SwaggerUI.SwaggerUIOptions>(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Publisher API v1");
    options.RoutePrefix = string.Empty;
    options.DefaultModelsExpandDepth(1);
    options.DefaultModelExpandDepth(2);
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    options.DisplayOperationId();
    options.ShowExtensions();
});

// Add CORS if needed
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

// Now we can safely get the logger after building the app
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Publisher API application...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("RabbitMQ configuration registered");
logger.LogInformation("RabbitMQ ConnectionFactory configured");
logger.LogInformation("DbContext configured with transient error resiliency");
logger.LogInformation("Product Repository registered");
logger.LogInformation("Controllers registered");
logger.LogInformation("Swagger/OpenAPI configured");
logger.LogInformation("CORS configured");
logger.LogInformation("Building web application...");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    logger.LogInformation("Development environment detected - enabling detailed error pages");
}

// Use Swagger in all environments for this demo
// In production, you would typically restrict this
app.UseSwagger(options =>
{
    options.RouteTemplate = "swagger/{documentName}/swagger.json";
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Publisher API v1");
    options.RoutePrefix = string.Empty;
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    options.DefaultModelsExpandDepth(1);
    options.DefaultModelExpandDepth(2);
    options.DisplayOperationId();
    options.ShowExtensions();
    options.DocumentTitle = "Publisher API - Swagger UI";
    options.EnableValidator();
    options.EnablePersistAuthorization();
});

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

logger.LogInformation("Publisher API configured and ready to start");
logger.LogInformation("Swagger UI available at: https://localhost:7000");

app.Run();
