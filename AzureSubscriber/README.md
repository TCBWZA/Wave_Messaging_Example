# AzureSubscriber - Background Service for Azure Service Bus

This is a .NET console application that listens to **Azure Service Bus** and processes domain events asynchronously, persisting them to the database.

## Overview

The Azure Subscriber service:
- ✅ Listens to Azure Service Bus Topics and Subscriptions
- ✅ Validates message payloads
- ✅ Persists data to SQL Server
- ✅ Automatically handles Dead Letter Queue for invalid messages
- ✅ Logs all operations with timestamps
- ✅ No local message broker needed

This is the cloud-based counterpart to **AzurePublisher**, providing enterprise-grade messaging without managing infrastructure.

## Architecture

```
[Azure Service Bus] (Cloud)
    ↓
[Azure Subscriber Service]
    ↓
Validation
    ↓
Database Operations
    ↓
✅ Success → Acknowledged
❌ Invalid → Dead Letter Queue
⚠️ Error → Automatic retry
```

## Prerequisites

- .NET 8 SDK or later
- Azure Subscription
- Azure Service Bus Namespace
- SQL Server (local or Azure SQL)
- Topic and Subscription created in Azure

## Getting Started

### 1. Create Azure Service Bus Resources

```bash
# Create namespace
az servicebus namespace create \
  --resource-group myResourceGroup \
  --name myServiceBusNamespace

# Create topic
az servicebus topic create \
  --resource-group myResourceGroup \
  --namespace-name myServiceBusNamespace \
  --name messaging-topic

# Create subscription
az servicebus topic subscription create \
  --resource-group myResourceGroup \
  --namespace-name myServiceBusNamespace \
  --topic-name messaging-topic \
  --name messaging-subscription

# Get connection string
az servicebus namespace authorization-rule keys list \
  --resource-group myResourceGroup \
  --namespace-name myServiceBusNamespace \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString \
  --output tsv
```

### 2. Clone and Setup

```bash
git clone https://github.com/your-org/project.git
cd project
```

### 3. Configure Environment

```bash
# Copy the environment template
cp .env.example .env

# Edit .env with your Azure details
# nano .env
```

**Key `.env` values needed:**
```
# Database (uses Windows Authentication)
DB_SERVER=localhost
DB_DATABASE=NeoWarehouse

# Azure Service Bus
AZURE_SERVICE_BUS_CONNECTION_STRING=Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key
AZURE_SERVICE_BUS_TOPIC_NAME=messaging-topic
AZURE_SERVICE_BUS_SUBSCRIPTION_NAME=messaging-subscription
```

**Important**: The `.env` file is automatically loaded at startup!

### 4. Build and Run

```bash
# Build the solution
dotnet build

# Run the Azure Subscriber (runs continuously)
dotnet run --project AzureSubscriber/AzureSubscriber.csproj
```

The Subscriber will display:
```
info: AzureSubscriberWorker[0]
      === Azure Subscriber Worker Started Successfully ===
info: AzureSubscriberWorker[0]
      Listening for messages on topic: messaging-topic, subscription: messaging-subscription
```

## Message Processing

### Supported Entity Types

| Entity | Create | Update | Source |
|--------|--------|--------|--------|
| Customer | ✅ | ✅ | Service Bus Topic |
| Order | ✅ | ✅ | Service Bus Topic |
| Product | ✅ | ✅ | Service Bus Topic |
| Supplier | ✅ | ✅ | Service Bus Topic |

### Processing Flow

1. **Service Bus delivers message** to subscription
2. **Subscriber receives message** from subscription
3. **Message deserialized** and validated
4. **Database operation** performed (create/update)
5. **Message completed** (removed from queue)
   - OR **sent to Dead Letter Queue** if invalid
   - OR **abandoned for retry** if error

## Error Handling

| Error Type | Action | Result |
|-----------|--------|--------|
| Validation Error | Sent to Dead Letter Queue | Permanent removal |
| Database Error | Abandoned for retry | Automatic requeue |
| System Error | Abandoned for retry | Automatic requeue |

## Running with Azure Publisher

### Terminal 1 - Azure Subscriber
```bash
cd project
cp .env.example .env
# Edit .env with Azure connection details
dotnet run --project AzureSubscriber/AzureSubscriber.csproj
```

Subscriber starts listening and waits for messages.

### Terminal 2 - Azure Publisher
```bash
cd project
cp .env.example .env
# Edit .env with same Azure details
dotnet run --project AzurePublisher/AzurePublisher.csproj
```

### Test Flow
1. Open Swagger: `https://localhost:7000`
2. Create an entity (customer, order, product, supplier)
3. Watch Subscriber process it
4. Verify in database

### Monitor Output
Terminal 1 will show:
```
info: === Message Received from Service Bus ===
info: Message details - Instruction: create, EntityType: Order
info: ✓ Order created successfully - ID: 42
info: 📧 Email confirmation has been sent to: customer@example.com
info: 🎫 Picking slip has been generated for Order ID: 42
info: Message acknowledged and completed
```

## Project Structure

```
AzureSubscriber/
├── Program.cs              # Entry point with ServiceBusProcessor
├── appsettings.json        # Default configuration
├── .env.example            # Environment template
└── AzureSubscriber.csproj  # Project file
```

## Configuration

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AzureServiceBus": {
    "ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;...",
    "TopicName": "messaging-topic",
    "SubscriptionName": "messaging-subscription"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=NeoWarehouse;Integrated Security=true;..."
  }
}
```

### .env File (Local)
```bash
AZURE_SERVICE_BUS_CONNECTION_STRING=Endpoint=sb://...
AZURE_SERVICE_BUS_TOPIC_NAME=messaging-topic
AZURE_SERVICE_BUS_SUBSCRIPTION_NAME=messaging-subscription
DB_SERVER=localhost
DB_DATABASE=NeoWarehouse
```

**Remember**: `.env` is in `.gitignore` - never commit it!

## Azure Portal Monitoring

### View Messages
```
Azure Portal
  → Service Bus Namespace
    → Topics
      → messaging-topic
        → Subscriptions
          → messaging-subscription
            → Properties: active message count, dead letter count
```

### Check Dead Letter Queue
Messages that fail validation appear in Dead Letter Queue:
```
Topic Deadletter Entities
  → Right-click to inspect failed messages
  → Helps debug validation issues
```

### Monitor Metrics
- Active message count
- Dead letter message count
- Processing rate
- Peak throughput

## Troubleshooting

### No Messages Arriving
1. Verify topic exists: `az servicebus topic show ...`
2. Verify subscription exists: `az servicebus topic subscription show ...`
3. Check connection string in `.env`
4. Ensure Publisher is running and sending messages
5. Check Azure Portal message count

### Connection String Error
```
Error: Invalid connection string format
```
- Copy full string from Azure Portal: Service Bus Namespace → Shared access policies → RootManageSharedAccessKey
- Ensure no extra spaces or line breaks
- Verify `SharedAccessKey` is complete

### Cannot Connect to Azure
- Check internet connection
- Verify Azure credentials
- Check firewall/network settings
- Try accessing Azure Portal to confirm access

### Database Connection Failed
1. Ensure SQL Server is running
2. Verify connection string in `.env`
3. Check database `NeoWarehouse` exists
4. Verify Windows Authentication is enabled

### Validation Errors
Messages with validation errors go to Dead Letter Queue:
```
info: ❌ Validation failed - Customer 'name' cannot be empty
info: 🗑️  Message sent to dead letter queue
```

Check validation rules in logs or source code.

## Performance Considerations

| Setting | Default | Notes |
|---------|---------|-------|
| Message Prefetch | Auto | Azure SDK handles optimization |
| Processing | Sequential | One message at a time |
| Timeout | 5 minutes | Before automatic requeue |
| Batch Size | 1 | For reliability |

## Best Practices

✅ **DO**
- Keep Subscriber running continuously
- Monitor Dead Letter Queue regularly
- Review logs for errors
- Use Azure Key Vault in production
- Set up alerts for message failures

❌ **DON'T**
- Stop Subscriber without warning
- Ignore Dead Letter Queue messages
- Commit `.env` file
- Share connection strings
- Process messages manually

## Cost Optimization

- Standard tier usually sufficient for dev/test
- Premium tier for high throughput
- Monitor message volume
- Clean up old dead letter messages
- Use autoscale if available

## Scaling

For production deployment:
1. **Use Azure Container Instances** or **App Service**
2. **Multiple instances** of Subscriber can scale horizontally
3. Each instance processes messages from subscription
4. Load balanced by Azure Service Bus
5. Monitor and autoscale based on queue depth

## Next Steps

1. **Verify Azure Resources** - Check in Azure Portal
2. **Start Subscriber** - Begin listening
3. **Publish Messages** - Use AzurePublisher
4. **Monitor Flow** - Check logs
5. **Verify Database** - Query results
6. **Set Up Alerts** - Get notified of issues

## Support

- 📖 [CONTRIBUTING.md](../CONTRIBUTING.md) - Coding guidelines
- 🔒 [SECURITY.md](../SECURITY.md) - Security best practices
- ❓ Open an issue for questions
- 🌐 [Azure Service Bus Docs](https://docs.microsoft.com/azure/service-bus)
- 🌐 [Azure Monitoring Docs](https://docs.microsoft.com/azure/azure-monitor)

---

**Cloud-Enabled Subscriber!** ☁️ 🚀
