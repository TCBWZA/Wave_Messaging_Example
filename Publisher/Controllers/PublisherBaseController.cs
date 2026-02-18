using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text.Json;

namespace Publisher.Controllers;

/// <summary>
/// Base controller for publishing domain events to RabbitMQ.
/// Provides common functionality for serializing and publishing messages.
/// </summary>
public abstract class PublisherBaseController : ControllerBase
{
    protected readonly IConnectionFactory _connectionFactory;
    protected readonly ILogger _logger;

    protected PublisherBaseController(IConnectionFactory connectionFactory, ILogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Publishes a message to RabbitMQ.
    /// </summary>
    /// <param name="exchangeName">The RabbitMQ exchange name</param>
    /// <param name="routingKey">The RabbitMQ routing key</param>
    /// <param name="message">The message object to publish</param>
    protected async Task PublishMessageAsync(string exchangeName, string routingKey, object message)
    {
        _logger.LogInformation("=== Publishing Message to RabbitMQ ===");
        _logger.LogDebug("Exchange: {ExchangeName}, Routing Key: {RoutingKey}", exchangeName, routingKey);

        try
        {
            _logger.LogDebug("Creating RabbitMQ connection...");
            using var connection = await _connectionFactory.CreateConnectionAsync();
            _logger.LogDebug("RabbitMQ connection created");

            using var channel = await connection.CreateChannelAsync();
            _logger.LogDebug("RabbitMQ channel created");

            // Declare the exchange (if it doesn't exist)
            _logger.LogDebug("Declaring exchange: {ExchangeName} (Direct, Durable)", exchangeName);
            await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Direct, durable: true);
            _logger.LogDebug("Exchange declared successfully");

            // Serialize the message to JSON
            _logger.LogDebug("Serializing message to JSON...");
            var messageJson = JsonSerializer.Serialize(message);
            _logger.LogDebug("Message serialized - Length: {Length} bytes", messageJson.Length);
            _logger.LogDebug("Message content: {Message}", messageJson);

            var messageBytes = System.Text.Encoding.UTF8.GetBytes(messageJson);

            // Create BasicProperties with DeliveryMode set to Persistent (2)
            var basicProperties = new RabbitMQ.Client.BasicProperties
            {
                DeliveryMode = RabbitMQ.Client.DeliveryModes.Persistent,
                ContentType = "application/json"
            };

            // Publish the message
            _logger.LogInformation("Publishing message to exchange '{Exchange}' with routing key '{RoutingKey}'", exchangeName, routingKey);
            await channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: basicProperties,
                body: messageBytes);

            _logger.LogInformation("=== Message Published Successfully ===");
            _logger.LogInformation("Message published to exchange '{Exchange}' with routing key '{RoutingKey}' - Size: {Size} bytes", exchangeName, routingKey, messageBytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "=== ERROR Publishing Message ===");
            _logger.LogError("Exchange: {Exchange}, Routing Key: {RoutingKey}", exchangeName, routingKey);
            _logger.LogError("Error details: {Message}", ex.Message);
            throw;
        }
    }
}
