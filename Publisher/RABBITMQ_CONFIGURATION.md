# RabbitMQ Configuration Guide

## 📋 Table of Contents

1. [Quick Start](#quick-start)
2. [appsettings.json Configuration](#appsettingsjson-configuration)
3. [RabbitMQ Setup Details](#rabbitmq-setup-details)
4. [Docker Commands](#docker-commands)
5. [RabbitMQ Management UI](#rabbitmq-management-ui)
6. [Testing the Setup](#testing-the-setup)
7. [Troubleshooting](#troubleshooting)

---

## Quick Start

### Start RabbitMQ with Docker

```bash
docker run -d --hostname rabbitmq-local --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management
```

**Ports Exposed:**
- **5672** - AMQP Protocol (for your application)
- **15672** - Management UI (for monitoring)

---

## appsettings.json Configuration

### Complete Publisher Configuration

Create or update `Publisher/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MessagingExampleDb;Trusted_Connection=true;"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest",
    "ExchangeName": "messaging_exchange",
    "QueueNamePrefix": "messaging_queue"
  }
}
```

### Development Configuration

Create `Publisher/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

### Production Configuration (Example)

For production with remote RabbitMQ:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "RabbitMQ": {
    "HostName": "rabbitmq.example.com",
    "Port": "5672",
    "UserName": "production_user",
    "Password": "secure_password_here"
  }
}
```

---

## RabbitMQ Setup Details

### Automatic Setup by Your Code

Your Publisher and Subscriber automatically create and configure:

#### Exchange Configuration

| Property | Value |
|----------|-------|
| **Name** | `messaging_exchange` |
| **Type** | `Direct` |
| **Durable** | Yes |
| **Auto-delete** | No |

#### Queue Configuration

| Property | Value |
|----------|-------|
| **Name** | `messaging_queue` |
| **Durable** | Yes |
| **Exclusive** | No |
| **Auto-delete** | No |

#### Queue-to-Exchange Bindings

The queue is bound to the exchange with these routing keys:

```
messaging_queue ← customer.*
messaging_queue ← order.*
messaging_queue ← product.*
messaging_queue ← supplier.*
messaging_queue ← telephonenumber.*
```

#### Routing Keys Used

**Customer Operations:**
- `customer.create` - When customer is created
- `customer.update` - When customer is updated

**Order Operations:**
- `order.create` - When order is created
- `order.update` - When order is updated

**Product Operations:**
- `product.create` - When product is created
- `product.update` - When product is updated

**Supplier Operations:**
- `supplier.create` - When supplier is created
- `supplier.update` - When supplier is updated

**Telephone Number Operations:**
- `telephonenumber.create` - When telephone number is created
- `telephonenumber.update` - When telephone number is updated

---

## Docker Commands

### Start RabbitMQ

```bash
docker run -d --hostname rabbitmq-local --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management
```

### Stop RabbitMQ

```bash
docker stop rabbitmq
```

### Start Existing RabbitMQ Container

```bash
docker start rabbitmq
```

### Remove RabbitMQ Container

```bash
docker rm rabbitmq
```

### View RabbitMQ Logs

```bash
docker logs rabbitmq
```

### Check if RabbitMQ is Running

```bash
docker ps | grep rabbitmq
```

### Access RabbitMQ Shell

```bash
docker exec -it rabbitmq bash
```

---

## RabbitMQ Management UI

### Access the Management Console

1. **URL**: http://localhost:15672
2. **Default Credentials**:
   - Username: `guest`
   - Password: `guest`

### Viewing Published Messages

**Step 1: Check the Exchange**
- Click **"Exchanges"** tab
- Find `messaging_exchange`
- Click on it to view details
- Confirm it's **Direct** type and **Durable**

**Step 2: Set Up a Test Queue** (to see messages)
Exchanges don't store messages - queues do. You need to bind a queue to see messages.

1. Click **"Queues"** tab
2. Click **"Add a new queue"**
3. Queue name: `test.customer.queue`
4. Durability: **Durable**
5. Click **"Add Queue"**

**Step 3: Bind Queue to Exchange**
1. Go back to **"Exchanges"** tab
2. Click on `messaging_exchange`
3. Scroll to **"Bindings"** section
4. Under "Bind queue to this exchange":
   - Queue name: `test.customer.queue`
   - Routing key: `customer.create`
   - Click **"Bind"**

**Step 4: Publish a Test Message**
1. Use the Publisher API to create a customer:
   - POST to `/api/customers`
   - Body:
   ```json
   {
     "name": "Test Customer",
     "email": "test@example.com",
     "phoneNumbers": []
   }
   ```

**Step 5: View the Message**
1. Click **"Queues"** tab
2. Click on `test.customer.queue`
3. Scroll down to **"Get Messages"** section
4. Click **"Get Message(s)"** button
5. Your published message should appear here!

---

## Queue and Binding Setup

### Complete List of Queues to Create

All queues should be created as **Durable** in RabbitMQ.

| # | Queue Name | Purpose | Durable | Auto-Delete |
|---|-----------|---------|---------|-------------|
| 1 | `customer.queue` | Consume customer create/update messages | ✅ Yes | ❌ No |
| 2 | `order.queue` | Consume order create/update messages | ✅ Yes | ❌ No |
| 3 | `product.queue` | Consume product create/update messages | ✅ Yes | ❌ No |
| 4 | `supplier.queue` | Consume supplier create/update messages | ✅ Yes | ❌ No |

---

### Complete List of Bindings to Create

All bindings connect queues to `messaging_exchange` (Direct type).

#### Customer Queue Bindings

| Queue | Exchange | Routing Key |
|-------|----------|-----------|
| `customer.queue` | `messaging_exchange` | `customer.create` |
| `customer.queue` | `messaging_exchange` | `customer.update` |

#### Order Queue Bindings

| Queue | Exchange | Routing Key |
|-------|----------|-----------|
| `order.queue` | `messaging_exchange` | `order.create` |
| `order.queue` | `messaging_exchange` | `order.update` |

#### Product Queue Bindings

| Queue | Exchange | Routing Key |
|-------|----------|-----------|
| `product.queue` | `messaging_exchange` | `product.create` |
| `product.queue` | `messaging_exchange` | `product.update` |

#### Supplier Queue Bindings

| Queue | Exchange | Routing Key |
|-------|----------|-----------|
| `supplier.queue` | `messaging_exchange` | `supplier.create` |
| `supplier.queue` | `messaging_exchange` | `supplier.update` |

---

### Manual Setup via RabbitMQ Management UI

#### Create Customer Queue

1. Go to **Queues** tab
2. Click **"Add a new queue"**
3. Fill in:
   - **Name**: `customer.queue`
   - **Durability**: **Durable**
4. Click **"Add Queue"**

#### Create Order Queue

1. Go to **Queues** tab
2. Click **"Add a new queue"**
3. Fill in:
   - **Name**: `order.queue`
   - **Durability**: **Durable**
4. Click **"Add Queue"**

#### Create Product Queue

1. Go to **Queues** tab
2. Click **"Add a new queue"**
3. Fill in:
   - **Name**: `product.queue`
   - **Durability**: **Durable**
4. Click **"Add Queue"**

#### Create Supplier Queue

1. Go to **Queues** tab
2. Click **"Add a new queue"**
3. Fill in:
   - **Name**: `supplier.queue`
   - **Durability**: **Durable**
4. Click **"Add Queue"**

---

### Bind Customer Queue to Exchange

1. Go to **Exchanges** tab
2. Click on `messaging_exchange`
3. Scroll to **"Bindings"** section
4. Under "Bind queue to this exchange":
   - **Queue name**: `customer.queue`
   - **Routing key**: `customer.create`
   - Click **"Bind"**
5. Repeat for `customer.update` routing key

---

### Bind Order Queue to Exchange

1. Go to **Exchanges** tab
2. Click on `messaging_exchange`
3. Scroll to **"Bindings"** section
4. Under "Bind queue to this exchange":
   - **Queue name**: `order.queue`
   - **Routing key**: `order.create`
   - Click **"Bind"**
5. Repeat for `order.update` routing key

---

### Bind Product Queue to Exchange

1. Go to **Exchanges** tab
2. Click on `messaging_exchange`
3. Scroll to **"Bindings"** section
4. Under "Bind queue to this exchange":
   - **Queue name**: `product.queue`
   - **Routing key**: `product.create`
   - Click **"Bind"**
5. Repeat for `product.update` routing key

---

### Bind Supplier Queue to Exchange

1. Go to **Exchanges** tab
2. Click on `messaging_exchange`
3. Scroll to **"Bindings"** section
4. Under "Bind queue to this exchange":
   - **Queue name**: `supplier.queue`
   - **Routing key**: `supplier.create`
   - Click **"Bind"**
5. Repeat for `supplier.update` routing key

---

### Setup Via RabbitMQ HTTP API (Alternative)

Use the RabbitMQ HTTP Management API with PowerShell commands:

```powershell
# RabbitMQ credentials
$credentials = New-Object System.Management.Automation.PSCredential(
    "guest",
    (ConvertTo-SecureString "guest" -AsPlainText -Force)
)

# RabbitMQ Management API base URL
$baseUrl = "http://localhost:15672/api"

# ============================================
# CREATE QUEUES
# ============================================

# Create Customer Queue
Invoke-RestMethod -Uri "$baseUrl/queues/%2F/customer.queue" `
    -Credential $credentials `
    -Method Put `
    -ContentType "application/json" `
    -Body '{"durable":true}'

# Create Order Queue
Invoke-RestMethod -Uri "$baseUrl/queues/%2F/order.queue" `
    -Credential $credentials `
    -Method Put `
    -ContentType "application/json" `
    -Body '{"durable":true}'

# Create Product Queue
Invoke-RestMethod -Uri "$baseUrl/queues/%2F/product.queue" `
    -Credential $credentials `
    -Method Put `
    -ContentType "application/json" `
    -Body '{"durable":true}'

# Create Supplier Queue
Invoke-RestMethod -Uri "$baseUrl/queues/%2F/supplier.queue" `
    -Credential $credentials `
    -Method Put `
    -ContentType "application/json" `
    -Body '{"durable":true}'

# ============================================
# CREATE BINDINGS
# ============================================

# Bind Customer Queue - customer.create
Invoke-RestMethod -Uri "$baseUrl/bindings/%2F/e/messaging_exchange/q/customer.queue" `
    -Credential $credentials `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"routing_key":"customer.create"}'

# Bind Customer Queue - customer.update
Invoke-RestMethod -Uri "$baseUrl/bindings/%2F/e/messaging_exchange/q/customer.queue" `
    -Credential $credentials `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"routing_key":"customer.update"}'

# Bind Order Queue - order.create
Invoke-RestMethod -Uri "$baseUrl/bindings/%2F/e/messaging_exchange/q/order.queue" `
    -Credential $credentials `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"routing_key":"order.create"}'

# Bind Order Queue - order.update
Invoke-RestMethod -Uri "$baseUrl/bindings/%2F/e/messaging_exchange/q/order.queue" `
    -Credential $credentials `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"routing_key":"order.update"}'

# Bind Product Queue - product.create
Invoke-RestMethod -Uri "$baseUrl/bindings/%2F/e/messaging_exchange/q/product.queue" `
    -Credential $credentials `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"routing_key":"product.create"}'

# Bind Product Queue - product.update
Invoke-RestMethod -Uri "$baseUrl/bindings/%2F/e/messaging_exchange/q/product.queue" `
    -Credential $credentials `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"routing_key":"product.update"}'

# Bind Supplier Queue - supplier.create
Invoke-RestMethod -Uri "$baseUrl/bindings/%2F/e/messaging_exchange/q/supplier.queue" `
    -Credential $credentials `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"routing_key":"supplier.create"}'

# Bind Supplier Queue - supplier.update
Invoke-RestMethod -Uri "$baseUrl/bindings/%2F/e/messaging_exchange/q/supplier.queue" `
    -Credential $credentials `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"routing_key":"supplier.update"}'

Write-Host "All queues and bindings created successfully!" -ForegroundColor Green
```

**Expected Response:**
- Queue creation: `201 Created`
- Binding creation: `201 Created`

**Alternative: Compact PowerShell Script**

Save as `setup-rabbitmq.ps1`:

```powershell
param(
    [string]$RabbitMqHost = "localhost",
    [int]$RabbitMqPort = 15672,
    [string]$Username = "guest",
    [string]$Password = "guest"
)

# Create credentials
$credentials = New-Object System.Management.Automation.PSCredential(
    $Username,
    (ConvertTo-SecureString $Password -AsPlainText -Force)
)

$baseUrl = "http://${RabbitMqHost}:${RabbitMqPort}/api"

# Queues to create
$queues = @("customer.queue", "order.queue", "product.queue", "supplier.queue")

# Bindings to create (queue, routing_key)
$bindings = @(
    ("customer.queue", "customer.create"),
    ("customer.queue", "customer.update"),
    ("order.queue", "order.create"),
    ("order.queue", "order.update"),
    ("product.queue", "product.create"),
    ("product.queue", "product.update"),
    ("supplier.queue", "supplier.create"),
    ("supplier.queue", "supplier.update")
)

Write-Host "Creating RabbitMQ queues..." -ForegroundColor Cyan

# Create queues
foreach ($queue in $queues) {
    try {
        Invoke-RestMethod -Uri "$baseUrl/queues/%2F/$queue" `
            -Credential $credentials `
            -Method Put `
            -ContentType "application/json" `
            -Body '{"durable":true}' | Out-Null
        Write-Host "✓ Created queue: $queue" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ Failed to create queue: $queue" -ForegroundColor Red
        Write-Host "  Error: $_" -ForegroundColor Red
    }
}

Write-Host "`nCreating RabbitMQ bindings..." -ForegroundColor Cyan

# Create bindings
foreach ($binding in $bindings) {
    $queue = $binding[0]
    $routingKey = $binding[1]
    try {
        Invoke-RestMethod -Uri "$baseUrl/bindings/%2F/e/messaging_exchange/q/$queue" `
            -Credential $credentials `
            -Method Post `
            -ContentType "application/json" `
            -Body "{`"routing_key`":`"$routingKey`"}" | Out-Null
        Write-Host "✓ Created binding: $queue -> $routingKey" -ForegroundColor Green
    }
    catch {
        Write-Host "✗ Failed to create binding: $queue -> $routingKey" -ForegroundColor Red
        Write-Host "  Error: $_" -ForegroundColor Red
    }
}

Write-Host "`n✓ RabbitMQ setup complete!" -ForegroundColor Green
```

**Run the script:**

```powershell
# Using default settings (localhost, guest/guest)
.\setup-rabbitmq.ps1

# Using custom settings
.\setup-rabbitmq.ps1 -RabbitMqHost "rabbitmq.example.com" -RabbitMqPort 15672 -Username "admin" -Password "password"
```

---

### Expected Queue Bindings for Full Application

When the Subscriber is running, it automatically declares these queues and bindings:

| Queue Name | Exchange | Routing Keys | Purpose |
|-----------|----------|-----------|---------|
| `customer.queue` | `messaging_exchange` | `customer.create`, `customer.update` | Consume customer messages |
| `order.queue` | `messaging_exchange` | `order.create`, `order.update` | Consume order messages |
| `product.queue` | `messaging_exchange` | `product.create`, `product.update` | Consume product messages |
| `supplier.queue` | `messaging_exchange` | `supplier.create`, `supplier.update` | Consume supplier messages |

---

## RabbitMQ Management UI

### Access the Management Console

```
http://localhost:15672
```

### Default Credentials

| Field | Value |
|-------|-------|
| **Username** | `guest` |
| **Password** | `guest` |

### What to Monitor

#### Exchanges Tab
- View `messaging_exchange` configuration
- Check exchange type and durability
- Verify bindings

#### Queues Tab
- View `messaging_queue` details
- Check message count
- Monitor ready/unacked message counts
- View consumer count

#### Connections Tab
- See active connections from Publisher and Subscriber
- Monitor connection status

#### Channels Tab
- View active channels
- Check prefetch counts and other settings

#### Admin Tab (if needed)
- Manage users
- Set permissions
- Configure policies

---

## Testing the Setup

### Step 1: Start RabbitMQ

```bash
docker run -d --hostname rabbitmq-local --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management
```

Wait 5-10 seconds for RabbitMQ to start.

### Step 2: Run Publisher API

```bash
dotnet run --project Publisher/Publisher.csproj
```

Expected output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7000
```

### Step 3: Open Swagger UI

Navigate to:
```
https://localhost:7000
```

### Step 4: Create a Customer via Swagger

1. Click on **Customers** section
2. Click on **POST /api/customers**
3. Click **Try it out**
4. Enter this JSON:

```json
{
  "name": "Test Company",
  "email": "test@example.com",
  "phoneNumbers": [
    {
      "type": "Work",
      "number": "+1-555-0100"
    }
  ]
}
```

5. Click **Execute**
6. Verify response is `201 Created`

### Step 5: Check RabbitMQ UI

1. Open http://localhost:15672
2. Login with guest/guest
3. Go to **Queues** tab
4. Click on `messaging_queue`
5. You should see message count increased

### Step 6: Run Subscriber

```bash
dotnet run --project Subscriber/Subscriber.csproj
```

Expected output:
```
info: Subscriber.SubscriberWorker[0]
      Subscriber Worker started. Listening for messages...
```

### Step 7: Monitor Message Processing

1. Watch Subscriber console for:
   ```
   Message received: {...}
   Processing customer message: create
   ```

2. Check RabbitMQ UI - message count should decrease

### Step 8: Verify Database

Check SQL Server to confirm customer was created in the database.

---

## Troubleshooting

### Issue: "Cannot connect to RabbitMQ"

**Solution:**
1. Verify RabbitMQ is running:
   ```bash
   docker ps | grep rabbitmq
   ```

2. If not running, start it:
   ```bash
   docker start rabbitmq
   ```

3. Check RabbitMQ logs:
   ```bash
   docker logs rabbitmq
   ```

4. Verify connection settings in `appsettings.json`:
   - HostName: `localhost`
   - Port: `5672`
   - UserName: `guest`
   - Password: `guest`

### Issue: "Connection refused on port 5672"

**Solution:**
1. Ensure Docker is running
2. Check if port 5672 is already in use:
   ```bash
   netstat -ano | findstr :5672
   ```

3. If port is in use, stop the conflicting service or use a different port

### Issue: "No messages appearing in queue"

**Solution:**
1. Verify exchange exists:
   - RabbitMQ UI → Exchanges → `messaging_exchange`

2. Verify queue exists:
   - RabbitMQ UI → Queues → `messaging_queue`

3. Verify binding exists:
   - RabbitMQ UI → Queues → `messaging_queue` → Bindings

4. Check Publisher logs for errors

5. Try creating a new entity through Swagger UI

### Issue: "Publisher API won't start"

**Solution:**
1. Verify SQL Server connection string is correct
2. Check database exists: `MessagingExampleDb`
3. Verify RabbitMQ connection settings
4. Check that all required NuGet packages are installed

### Issue: "Swagger UI not loading"

**Solution:**
1. Verify Publisher is running on correct port (7000)
2. Check browser console for errors (F12)
3. Clear browser cache
4. Try in incognito/private mode
5. Check appsettings.json for syntax errors

### Issue: "401 Unauthorized on RabbitMQ UI"

**Solution:**
1. Verify credentials are correct:
   - Username: `guest`
   - Password: `guest`

2. Check if RabbitMQ container has these default permissions set

3. If needed, reset RabbitMQ:
   ```bash
   docker stop rabbitmq
   docker rm rabbitmq
   docker run -d --hostname rabbitmq-local --name rabbitmq \
     -p 5672:5672 \
     -p 15672:15672 \
     rabbitmq:3-management
   ```

---

## Connection Flow Diagram

```
Publisher API
    ↓
Swagger UI (https://localhost:7000)
    ↓
[POST /api/customers]
    ↓
Controller saves to SQL Server
    ↓
Controller publishes to RabbitMQ
    ↓
[messaging_exchange]
    ↓
[messaging_queue]
    ↓
Subscriber (listening)
    ↓
Subscriber processes message
    ↓
Subscriber saves to SQL Server
```

---

## Summary Checklist

- [ ] RabbitMQ running on localhost:5672
- [ ] RabbitMQ Management UI accessible at localhost:15672
- [ ] `appsettings.json` configured with RabbitMQ settings
- [ ] Publisher API running on https://localhost:7000
- [ ] Swagger UI accessible and showing all endpoints
- [ ] Test message created via Swagger
- [ ] Message visible in RabbitMQ queue
- [ ] Subscriber running and consuming messages
- [ ] Database updated with processed messages

---

## Quick Reference

| Item | Value |
|------|-------|
| **RabbitMQ AMQP** | `localhost:5672` |
| **RabbitMQ Management** | `http://localhost:15672` |
| **Publisher API** | `https://localhost:7000` |
| **Swagger UI** | `https://localhost:7000` |
| **Default Username** | `guest` |
| **Default Password** | `guest` |
| **Exchange Name** | `messaging_exchange` |
| **Queue Name** | `messaging_queue` |
| **Database** | `MessagingExampleDb` |

---

**Happy messaging! 🚀**
