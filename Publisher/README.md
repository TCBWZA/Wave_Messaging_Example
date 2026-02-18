# Publisher - REST API for Publishing Domain Events

This is an ASP.NET Core Web API that publishes domain events (customers, orders, products, suppliers) to message brokers for asynchronous processing.

## Overview

The Publisher service:
- ✅ Accepts HTTP requests with domain entity data
- ✅ Validates the data using FluentValidation
- ✅ Publishes messages to RabbitMQ (or Azure Service Bus)
- ✅ Returns **202 Accepted** immediately without waiting for persistence
- ✅ Provides Swagger UI for easy API testing

The actual database persistence happens in the **Subscriber** service, which consumes and processes the messages asynchronously.

## Architecture

```
HTTP Request
    ↓
[Publisher API]
    ↓
Validation
    ↓
[Message Broker] (RabbitMQ)
    ↓
[Subscriber] ← Consumes & Persists
    ↓
Database
```

## Prerequisites

- .NET 8 SDK or later
- SQL Server (local or remote)
- RabbitMQ (for messaging)
- Visual Studio 2022 or Visual Studio Code

## Getting Started

### 1. Clone and Setup

```bash
git clone https://github.com/your-org/project.git
cd project
```

### 2. Setup RabbitMQ Queues and Bindings

A setup script is included to automate RabbitMQ configuration:

**File**: `Publisher/setup-rabbitmq.txt`

**To use the setup script:**

```powershell
# Step 1: Rename the file to .ps1
cd Publisher
Rename-Item setup-rabbitmq.txt setup-rabbitmq.ps1

# Step 2: Run the setup script (PowerShell as Administrator)
.\setup-rabbitmq.ps1
```

**Or manually (if you prefer):**
```powershell
# Copy and rename
Copy-Item setup-rabbitmq.txt setup-rabbitmq.ps1

# Run it
.\setup-rabbitmq.ps1
```

The script will:
- ✅ Create the `messaging_exchange` topic exchange
- ✅ Create queues: `customer.queue`, `order.queue`, `product.queue`, `supplier.queue`
- ✅ Create bindings between exchange and queues with routing keys
- ✅ Configure all queues as durable and non-auto-delete

**Options:**
```powershell
# Use default values (localhost, guest/guest)
.\setup-rabbitmq.ps1

# Or specify custom values
.\setup-rabbitmq.ps1 -rabbitmqHost "192.168.1.100" -rabbitmqUser "admin" -rabbitmqPassword "secure-password"
```

**Prerequisites for setup script:**
- RabbitMQ installed and running
- RabbitMQ Management Plugin enabled (default port 15672)
- PowerShell with admin privileges

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

### 4. Build and Run

```bash
# Build the solution
dotnet build

# Run the Publisher
dotnet run --project Publisher/Publisher.csproj

# Publisher will start at: https://localhost:7000
```

## API Endpoints

### Customers
- **POST** `/api/customers` - Create a new customer
- **PUT** `/api/customers/{id}` - Update an existing customer
- **GET** `/api/customers/create` - Generate and create a fake customer (for testing)

### Orders
- **POST** `/api/orders` - Create a new order
- **PUT** `/api/orders/{id}` - Update an existing order
- **GET** `/api/orders/create` - Generate and create a fake order (for testing)

### Products
- **POST** `/api/products` - Create a new product
- **PUT** `/api/products/{id}` - Update an existing product
- **GET** `/api/products/create` - Generate and create a fake product (for testing)

### Suppliers
- **POST** `/api/suppliers` - Create a new supplier
- **PUT** `/api/suppliers/{id}` - Update an existing supplier
- **GET** `/api/suppliers/create` - Generate and create a fake supplier (for testing)

## Example Usage

### Create an Order
```bash
curl -X POST https://localhost:7000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
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
    },
    "orderItems": [
      {
        "productId": 1,
        "quantity": 2,
        "unitPrice": 29.99
      }
    ]
  }'
```

### Use Swagger UI
1. Open your browser: `https://localhost:7000`
2. Click on an endpoint
3. Click "Try it out"
4. Enter the request body
5. Click "Execute"

## Testing

### Run Unit Tests
```bash
dotnet test
```

### Generate Fake Data
Use the `/create` endpoints to generate fake data for testing:
```bash
# Create a fake customer
curl https://localhost:7000/api/customers/create

# Create a fake order
curl https://localhost:7000/api/orders/create

# Create a fake product
curl https://localhost:7000/api/products/create
```

## Project Structure

```
Publisher/
├── Controllers/          # API endpoints
│   ├── OrdersController.cs
│   ├── CustomersController.cs
│   ├── ProductsController.cs
│   └── SuppliersController.cs
├── Program.cs           # Application startup
├── appsettings.json     # Default configuration
└── Publisher.csproj     # Project file
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
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

### .env File
Environment variables override appsettings.json values. The `.env` file is loaded automatically at startup.

**Do NOT commit `.env`** - it contains your local secrets!

## Logging

Logs are output to the console with structured logging:
- 📧 Email confirmations are logged when orders are created
- 🎫 Picking slips are logged for warehouse operations
- ❌ Validation errors are logged with details
- ⚠️ Warnings and errors are tracked

## Troubleshooting

### Database Connection Failed
- Ensure SQL Server is running
- Verify connection string in `.env`
- Check Windows Authentication is enabled
- Run: `sqlcmd -S localhost -E` to test connection

### RabbitMQ Connection Failed
- Ensure RabbitMQ is running
- Check hostname and port in `.env`
- Verify RabbitMQ credentials

### Swagger UI Not Loading
- Clear browser cache
- Ensure HTTPS is enabled
- Check URL: `https://localhost:7000`

## Best Practices

✅ **DO**
- Use Swagger UI for testing
- Check logs for errors
- Use `/create` endpoints for testing
- Keep `.env` secure and out of version control

❌ **DON'T**
- Commit `.env` file
- Hardcode secrets in code
- Test in production
- Share connection strings

## RabbitMQ Setup Script

A PowerShell setup script is included to automate RabbitMQ configuration:

**File**: `Publisher/setup-rabbitmq.txt` (rename to `.ps1` before running)

**Setup Instructions:**

1. **Rename the file to .ps1:**
```powershell
cd Publisher
Rename-Item setup-rabbitmq.txt setup-rabbitmq.ps1
```

2. **Review the script contents** (optional but recommended):
```powershell
Get-Content setup-rabbitmq.ps1
```

3. **Run the script:**
```powershell
# With default values (localhost, guest/guest)
.\setup-rabbitmq.ps1

# Or with custom values
.\setup-rabbitmq.ps1 -rabbitmqHost "your-host" -rabbitmqUser "your-user" -rabbitmqPassword "your-pass"
```

**What the script creates:**
- Exchange: `messaging_exchange` (topic type)
- 4 Queues: customer, order, product, supplier
- 8 Bindings: create/update routing keys for each entity
- All queues are durable and persistent

**Verify Setup in RabbitMQ Management UI:**
1. Open: `http://localhost:15672`
2. Login with your credentials (default: guest/guest)
3. Go to **Exchanges** → `messaging_exchange`
4. Check **Bindings** to see all queue connections

---

## Next Steps

1. **Run Setup Script** - Configure RabbitMQ infrastructure
2. **Start the Subscriber** - It consumes and persists the messages
3. **Test with Swagger** - Create some test data
4. **Check the database** - Verify data was persisted
5. **Monitor logs** - Watch for email confirmations and picking slips

## Support

- 📖 Check [CONTRIBUTING.md](../CONTRIBUTING.md) for coding guidelines
- 🔒 See [SECURITY.md](../SECURITY.md) for security best practices
- ❓ Open an issue for questions

---

**Happy Publishing!** 🚀
