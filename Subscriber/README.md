# Subscriber - Background Service for Processing Domain Events

This is a .NET console application that listens to message queues (RabbitMQ) and processes domain events asynchronously, persisting them to the database.

## Overview

The Subscriber service:
- ✅ Listens to RabbitMQ queues for incoming messages
- ✅ Validates message payloads before processing
- ✅ Persists data to SQL Server database
- ✅ Sends invalid messages to Dead Letter Queue
- ✅ Logs all operations with detailed information
- ✅ Handles errors gracefully with automatic retries

This is the counterpart to the **Publisher** service, which publishes messages. Together they provide an asynchronous, loosely-coupled architecture.

## Architecture

```
[Message Queue] (RabbitMQ)
    ↓
[Subscriber Service] ← Listens continuously
    ↓
Validation
    ↓
Database Operations
    ↓
✅ Success or ❌ Dead Letter Queue
```

## Prerequisites

- .NET 8 SDK or later
- SQL Server (local or remote)
- RabbitMQ (running and accessible)
- Command-line interface (PowerShell, Bash, etc.)

## Getting Started

### 1. Clone and Setup

```bash
git clone https://github.com/your-org/project.git
cd project
```

### 2. Setup RabbitMQ (First Time Only)

Before running the Subscriber for the first time, you need to set up RabbitMQ queues and bindings.

**See [Publisher/README.md](../Publisher/README.md#rabbitmq-setup-script) for RabbitMQ setup instructions.**

The setup script (`setup-rabbitmq.txt` in the Publisher folder) creates:
- Exchange and queues for all entity types
- Bindings needed for both Publisher and Subscriber

### 3. Configure Environment

```bash
# Copy the environment template
cp .env.example .env

# Edit .env with your actual values
# nano .env  (or use your preferred editor)
```

**Key `.env` values needed:**
```
# Database (uses Windows Authentication)
DB_SERVER=localhost
DB_DATABASE=NeoWarehouse

# RabbitMQ
RABBITMQ_HOSTNAME=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
```

**Important**: The `.env` file is automatically loaded at startup. Make sure it exists before running!

### 4. Build and Run

```bash
# Build the solution
dotnet build

# Run the Subscriber (it will run continuously)
dotnet run --project Subscriber/Subscriber.csproj
```

The Subscriber will start listening and display:
```
info: SubscriberWorker[0]
      === Subscriber Worker Started Successfully ===
info: SubscriberWorker[0]
      Listening for messages on queues: customer.queue, order.queue, product.queue, supplier.queue
```

## Message Processing

### Supported Entity Types

| Entity Type | Create | Update | Queue |
|------------|--------|--------|-------|
| Customer | ✅ | ✅ | customer.queue |
| Order | ✅ | ✅ | order.queue |
| Product | ✅ | ✅ | product.queue |
| Supplier | ✅ | ✅ | supplier.queue |

### Processing Flow

1. **Message Arrives** → Logged with details
2. **Deserialization** → JSON parsed into domain objects
3. **Validation** → Payload validated against rules
4. **Database Operation** → Create or update entity
5. **Success** → Message acknowledged, removed from queue
6. **Failure** → Sent to Dead Letter Queue or retried

### Error Handling

| Error Type | Action |
|-----------|--------|
| Validation Error | ❌ Dead Letter Queue (permanent) |
| Database Error | ⚠️ Requeued for retry (automatic) |
| System Error | ⚠️ Requeued for retry (automatic) |

## Project Structure

```
Subscriber/
├── Program.cs              # Application entry point & configuration
├── appsettings.json        # Default configuration
├── .env.example            # Environment template (commit this)
├── .env                    # Your local config (DO NOT commit)
└── Subscriber.csproj       # Project file
```

## Key Features

### 🎫 Order Processing
When an order is created:
```
📧 Email confirmation has been sent to: customer@example.com
🎫 Picking slip has been generated for Order ID: 42
```

### ✅ Validation
- Customer name required and max 255 characters
- Email format validation
- Order items validated (positive quantities, valid prices)
- Product codes must be valid GUIDs

### 📊 Logging
All operations are logged with timestamps and details:
```
info: SubscriberWorker[0]
      === Message Received from Queue: order.queue ===
debug: SubscriberWorker[0]
      Raw message body: {...}
info: SubscriberWorker[0]
      ✓ Order created successfully - ID: 42, CustomerId: 1, Status: Received
```

### 🔄 Auto-Retry
Transient errors automatically retry without user intervention.

## Running with Publisher

### Setup (Terminal 1 - Subscriber)
```bash
cd project
cp .env.example .env
# Edit .env with your settings
dotnet run --project Subscriber/Subscriber.csproj
```

The Subscriber will start listening and wait for messages.

### Publish Messages (Terminal 2 - Publisher)
```bash
cd project
dotnet run --project Publisher/Publisher.csproj
```

Then use Swagger UI at `https://localhost:7000` to create entities.

### Monitor (Terminal 1)
Watch the Subscriber logs as messages arrive and are processed:
```
info: ✓ Order created successfully - ID: 42
info: 📧 Email confirmation has been sent to: customer@example.com
info: 🎫 Picking slip has been generated for Order ID: 42
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
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=NeoWarehouse;Integrated Security=true;..."
  }
}
```

### .env File (Local)
Environment variables override appsettings values:
```bash
DB_SERVER=localhost
DB_DATABASE=NeoWarehouse
RABBITMQ_HOSTNAME=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
```

**Important**: `.env` is in `.gitignore` - never commit it!

## Troubleshooting

### No Messages Arriving
1. Check RabbitMQ is running: `netstat -an | find "5672"`
2. Verify connection string in `.env`
3. Check queues exist in RabbitMQ Management UI
4. Ensure Publisher is running and publishing messages

### Database Connection Failed
1. Verify SQL Server is running
2. Check connection string in `.env`
3. Ensure database `NeoWarehouse` exists
4. Verify Windows Authentication is enabled

### Validation Errors
Messages with validation errors go to Dead Letter Queue:
```
❌ Validation failed - Customer 'name' cannot be empty
🗑️  Message permanently deleted from queue
```

Check the validation rules in the logs.

### Memory Issues
If Subscriber uses too much memory:
1. Restart the service
2. Check for very large messages
3. Monitor database connections

## Best Practices

✅ **DO**
- Keep Subscriber running continuously in production
- Monitor logs regularly
- Configure Dead Letter Queue handling
- Use structured logging for analysis

❌ **DON'T**
- Stop Subscriber without proper shutdown
- Modify messages in queues manually
- Ignore validation errors
- Commit `.env` file

## Performance Tips

- **Prefetch Count**: Set to 1 (default) for fair distribution
- **Batch Processing**: Messages processed one at a time for reliability
- **Connection Pooling**: Automatic with EntityFrameworkCore
- **Async/Await**: All I/O operations are asynchronous

## Monitoring

### Check Queue Status
```bash
# In RabbitMQ Management UI (http://localhost:15672)
# View queues, message counts, and consumers
```

### View Logs
```bash
# Filter for errors
# Look for ❌ and ⚠️ symbols
# Check timestamp and entity details
```

### Database Verification
```bash
# Query created entities
SELECT * FROM Customers;
SELECT * FROM Orders;
SELECT * FROM Products;
SELECT * FROM Suppliers;
```

## Next Steps

1. **Start Publisher** - Begin publishing messages
2. **Create Test Data** - Use Swagger UI
3. **Monitor Subscriber** - Watch processing in real-time
4. **Verify Database** - Check data was persisted
5. **Scale Up** - Run multiple instances

## Support

- 📖 Check [CONTRIBUTING.md](../CONTRIBUTING.md) for coding guidelines
- 🔒 See [SECURITY.md](../SECURITY.md) for security best practices
- ❓ Open an issue for questions

---

**Happy Subscribing!** 🚀
