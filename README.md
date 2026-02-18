# Azure Service Bus Messaging Example

This directory contains example implementations of a pub/sub messaging system using **Azure Service Bus** instead of RabbitMQ.

## Projects

### AzurePublisher
REST API that publishes domain events (Customer, Order, Product, Supplier) to Azure Service Bus Topics.

**Key Features:**
- HTTP endpoints for creating/updating domain entities
- Publishes messages to Azure Service Bus topics
- Returns 202 Accepted immediately without waiting for persistence
- Swagger UI for testing endpoints
- UTF-8 console encoding for Unicode emoji support

**Configuration:**
```json
"AzureServiceBus": {
  "ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key",
  "TopicName": "messaging-topic"
}
```

**Build & Run:**
```bash
dotnet run --project AzurePublisher
```

Access Swagger UI at: `http://localhost:5000`

### AzureSubscriber
Background service that consumes messages from Azure Service Bus and persists them to the database.

**Key Features:**
- Listens to Service Bus Topic Subscriptions
- Validates message payloads before database operations
- Sends invalid messages to dead letter queue
- Supports CRUD operations (create/update)
- Logs email confirmations and picking slip generation for orders
- UTF-8 console encoding for Unicode emoji support

**Configuration:**
```json
"AzureServiceBus": {
  "ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key",
  "TopicName": "messaging-topic",
  "SubscriptionName": "messaging-subscription"
}
```

**Build & Run:**
```bash
dotnet run --project AzureSubscriber
```

## Prerequisites

1. **Azure Service Bus Namespace**
   - Create in Azure Portal: https://portal.azure.com
   - Get connection string from "Shared access policies"

2. **Create Topic & Subscription**
   - Topic name: `messaging-topic`
   - Subscription name: `messaging-subscription`
   - Both will be auto-created by Azure SDK if not present

3. **SQL Server Database**
   - Update connection string in `appsettings.json`
   - Run EF Core migrations

## Message Format

All messages follow this structure:

```json
{
  "instruction": "create",
  "entityType": "Order",
  "id": 123,
  "payload": {
    "customerId": 1,
    "supplierId": 1,
    "orderDate": "2024-01-15T10:30:00",
    "customerEmail": "customer@example.com",
    "orderStatus": "Received",
    "billingAddress": {
      "street": "123 Main St",
      "city": "New York",
      "county": "NY",
      "postalCode": "10001",
      "country": "USA"
    }
  }
}
```

### Differences from RabbitMQ

| Feature | RabbitMQ | Azure Service Bus |
|---------|----------|-------------------|
| **Transport** | AMQP Protocol | REST/HTTPS |
| **Persistence** | Optional | Built-in (24h default) |
| **Dead Letter Queue** | Separate queue | Automatic DLQ |
| **Subscriptions** | Bindings | Native subscriptions |
| **Scalability** | Manual clustering | Managed scaling |
| **Security** | SASL/SSL | Azure AD, Managed Identity |
| **Client Library** | `RabbitMQ.Client` | `Azure.Messaging.ServiceBus` |

## Key Code Differences

### Publishing a Message

**RabbitMQ:**
```csharp
var factory = new ConnectionFactory();
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();
channel.BasicPublish(exchange, routingKey, null, body);
```

**Azure Service Bus:**
```csharp
var sender = client.CreateSender(topicName);
var message = new ServiceBusMessage(body);
await sender.SendMessageAsync(message);
```

### Subscribing to Messages

**RabbitMQ:**
```csharp
var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) => { /* process */ };
channel.BasicConsume(queue, autoAck: false, consumer);
```

**Azure Service Bus:**
```csharp
var processor = client.CreateProcessor(topicName, subscriptionName);
processor.ProcessMessageAsync += ProcessMessageAsync;
processor.ProcessErrorAsync += ProcessErrorAsync;
await processor.StartProcessingAsync();
```

## Logging & Configuration

Both applications use:
- **appsettings.json** for all configuration
- **Structured logging** with ILogger
- **UTF-8 console encoding** for emoji support
- **Configuration sections:**
  - `Logging` - Log levels and filters
  - `AzureServiceBus` - Connection and topic/subscription names
  - `ConnectionStrings` - Database connection

## Testing

1. **Start Subscriber** - Begins listening for messages
2. **Open Publisher Swagger** - http://localhost:5000
3. **Create an entity** - POST to /api/orders, /api/products, etc.
4. **Monitor logs** - Check both Publisher and Subscriber console output
5. **Verify database** - Check SQL Server for persisted data

## Error Handling

- **Validation errors** → Dead Letter Queue (permanent removal)
- **Transient errors** → Automatically retried by Service Bus
- **System errors** → Logged and message abandoned for retry

## Performance Considerations

- **Batch Processing** - Service Bus can process multiple messages concurrently
- **Message Sessions** - Available for ordering guarantees
- **Prefetch** - Configure for optimal throughput/latency tradeoff
- **Connection Pooling** - ServiceBusClient handles connection reuse

## Next Steps

1. Replace connection strings with your Azure Service Bus instance
2. Create the Topic and Subscription in Azure Portal
3. Test with Swagger UI
4. Monitor metrics in Azure Portal
5. Consider implementing monitoring with Application Insights
