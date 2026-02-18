# AzurePublisher - REST API Publishing to Azure Service Bus

This is an ASP.NET Core Web API that publishes domain events to **Azure Service Bus** for asynchronous processing. It's functionally equivalent to the RabbitMQ-based Publisher, but uses Azure's managed messaging service.

## Overview

The Azure Publisher service:
- ✅ Accepts HTTP requests with domain entity data
- ✅ Validates data using FluentValidation
- ✅ Publishes messages to Azure Service Bus Topics
- ✅ Returns **202 Accepted** immediately
- ✅ Provides Swagger UI for API testing
- ✅ No local message broker setup required

Azure Service Bus handles:
- ✅ Message persistence (24 hours by default)
- ✅ Automatic Dead Letter Queue for failed messages
- ✅ Subscription management
- ✅ Enterprise-grade security

## Architecture

```
HTTP Request
    ↓
[Azure Publisher API]
    ↓
Validation
    ↓
[Azure Service Bus] (Cloud-based)
    ↓
[Azure Subscriber] ← Consumes & Persists
    ↓
Database
```

## Prerequisites

- .NET 8 SDK or later
- Azure Subscription
- Azure Service Bus Namespace
- SQL Server (local or Azure SQL)
- Visual Studio 2022 or VS Code

## Getting Started

### 1. Create Azure Service Bus

```bash
# Using Azure CLI
az servicebus namespace create \
  --resource-group myResourceGroup \
  --name myServiceBusNamespace \
  --location eastus \
  --sku Standard

# Get connection string
az servicebus namespace authorization-rule keys list \
  --resource-group myResourceGroup \
  --namespace-name myServiceBusNamespace \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString \
  --output tsv
```

### 2. Create Topic and Subscription

```bash
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
```

### 3. Clone and Setup

```bash
git clone https://github.com/your-org/project.git
cd project
```

### 4. Configure Environment

```bash
# Copy the environment template
cp .env.example .env

# Edit .env with your Azure values
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

### 5. Build and Run

```bash
# Build the solution
dotnet build

# Run the Azure Publisher
dotnet run --project AzurePublisher/AzurePublisher.csproj

# API will start at: https://localhost:7000
```

## API Endpoints

Same endpoints as the RabbitMQ Publisher:

### Customers
- **POST** `/api/customers` - Create customer
- **PUT** `/api/customers/{id}` - Update customer
- **GET** `/api/customers/create` - Generate fake customer

### Orders
- **POST** `/api/orders` - Create order
- **PUT** `/api/orders/{id}` - Update order
- **GET** `/api/orders/create` - Generate fake order

### Products
- **POST** `/api/products` - Create product
- **PUT** `/api/products/{id}` - Update product
- **GET** `/api/products/create` - Generate fake product

### Suppliers
- **POST** `/api/suppliers` - Create supplier
- **PUT** `/api/suppliers/{id}` - Update supplier
- **GET** `/api/suppliers/create` - Generate fake supplier

## Azure Service Bus Features

### Message Persistence
- Messages automatically stored for 24 hours
- Allows asynchronous processing even if Subscriber is down
- Automatic replay when Subscriber restarts

### Dead Letter Queue
- Invalid messages automatically moved to DLQ
- Accessible in Azure Portal for inspection
- Helps debug validation issues

### Subscriptions
- Multiple subscribers can consume same topic
- Each subscription has own message queue
- Allows scaling to multiple consumers

### Security
- Connection string is secure
- Messages encrypted in transit
- Only authenticated subscribers can receive

## Running with Azure Subscriber

### Terminal 1 - Azure Subscriber
```bash
cd project
cp .env.example .env
# Edit .env with Azure connection details
dotnet run --project AzureSubscriber/AzureSubscriber.csproj
```

### Terminal 2 - Azure Publisher
```bash
cd project
dotnet run --project AzurePublisher/AzurePublisher.csproj
```

### Use Swagger UI
1. Open: `https://localhost:7000`
2. Try creating entities
3. Watch Azure Subscriber process them

## Monitoring in Azure Portal

### View Topics and Subscriptions
```
Service Bus Namespace
  → Topics
    → messaging-topic
      → Subscriptions
        → messaging-subscription
          → Messages (count, size, details)
```

### View Dead Letter Queue
```
Topic Deadletter Entities
  → Shows failed messages
  → Helps debug validation issues
```

### Monitor Metrics
- Active message count
- Dead letter message count
- Processing rate
- Latency

## Project Structure

```
AzurePublisher/
├── Controllers/          # API endpoints
├── Program.cs           # Application startup with ServiceBusClient
├── appsettings.json     # Default configuration
├── .env.example         # Environment template
└── AzurePublisher.csproj
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
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=NeoWarehouse;Integrated Security=true;..."
  },
  "AzureServiceBus": {
    "ConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;...",
    "TopicName": "messaging-topic"
  }
}
```

### .env File
```bash
AZURE_SERVICE_BUS_CONNECTION_STRING=Endpoint=sb://...
AZURE_SERVICE_BUS_TOPIC_NAME=messaging-topic
DB_SERVER=localhost
DB_DATABASE=NeoWarehouse
```

## Azure vs RabbitMQ

| Feature | RabbitMQ | Azure Service Bus |
|---------|----------|-------------------|
| Setup | Local installation | Managed cloud service |
| Persistence | Optional | Automatic (24h) |
| Dead Letter | Manual setup | Automatic |
| Security | Basic auth | Azure AD, Managed Identity |
| Scaling | Manual | Automatic |
| Monitoring | Limited | Full Azure Portal |
| Cost | Self-hosted (free) | Pay per message |
| Best for | Development | Production, Enterprise |

## Troubleshooting

### Connection String Invalid
```
Error: Endpoint format not recognized
```
- Copy full connection string from Azure Portal
- Ensure no extra spaces
- Verify SharedAccessKey is not truncated

### Topic/Subscription Not Found
- Create topic in Azure Portal
- Create subscription under topic
- Verify names match .env values

### Authentication Failed
- Check connection string is current
- Regenerate keys if needed
- Verify Azure account has permissions

### No Messages Arriving at Subscriber
1. Check topic has messages (Azure Portal)
2. Verify subscription exists
3. Check .env values match
4. Review Subscriber logs

## Best Practices

✅ **DO**
- Use Azure Key Vault for connection strings in production
- Monitor Dead Letter Queue
- Set up alerts for failed messages
- Use Managed Identity when available
- Test connection before deploying

❌ **DON'T**
- Commit connection strings
- Store secrets in appsettings
- Use overly permissive access policies
- Ignore Dead Letter Queue messages

## Performance Optimization

- **Message Size**: Keep under 1 MB
- **Batch Operations**: Use when possible
- **Connection Reuse**: ServiceBusClient handles this
- **Partition Keys**: Use for ordered processing

## Cost Optimization

- Standard tier is usually sufficient
- Monitor message count
- Delete old dead letter messages
- Use autoscale if available

## Next Steps

1. **Start Azure Subscriber** - Begins consuming messages
2. **Create Test Data** - Use Swagger UI
3. **Monitor in Portal** - Watch message flow
4. **Check Database** - Verify persistence
5. **Set Up Alerts** - Get notified of failures

## Support

- 📖 Check [CONTRIBUTING.md](../CONTRIBUTING.md) for coding guidelines
- 🔒 See [SECURITY.md](../SECURITY.md) for security best practices
- ❓ Open an issue for questions
- 🌐 Azure Service Bus Docs: https://docs.microsoft.com/azure/service-bus

---

**Azure Ready!** ☁️ 🚀
