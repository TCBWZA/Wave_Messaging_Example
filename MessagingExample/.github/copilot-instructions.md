# GitHub Copilot Instructions for Messaging + Domain Model Project (.NET 8)

## Purpose

This repository demonstrates a clean, real-world Publish/Subscribe (pub/sub) messaging pattern using:

- .NET 8
- ASP.NET Core Web API (Publisher)
- RabbitMQ (running locally via Docker)
- JSON messages containing domain objects
- RESTful API endpoints for creating and updating messages

The goal is to provide students with a clear, understandable messaging example while keeping the domain model and data access layer manually controlled.

---

# 1. Solution Architecture (Publisher API and Subscriber in Separate Projects)

This solution uses **separate projects** for the Publisher and Subscriber to reflect real-world distributed messaging patterns.

## Project Structure

```
Domain/                 # Domain models (manually created)
Application/            # DTOs, Repositories, Validators, Mappings (manually created)
Infrastructure/         # EF Core DbContext, RabbitMQ helpers, Repository implementations
Publisher/              # ASP.NET Core Web API for publishing messages
Subscriber/             # Console app or Worker Service for consuming messages
```

Copilot must follow this structure when generating code.

---

# 2. Domain Models (Manually Created)

The domain models for this project are created manually by the developer.

These models include:

- `Supplier`
- `Order`
- `Product`
- `Customer`
- `Address`
- `OrderItem`
- `OrderStatus` (enum)
- `TelephoneNumber`

Copilot must **not** generate, modify, or restructure these model classes unless explicitly instructed.

### Model Conventions

- Models are POCO classes
- Value objects such as `Address` and `TelephoneNumber` are manually implemented
- `OrderStatus` is a manually defined enum
- Navigation properties and relationships are defined manually
- Copilot must **not** add or modify navigation properties, foreign keys, or relationships

---

# 3. EF Core DbContext (Already Exists)

A custom EF Core DbContext exists and includes:

- `DbSet<Supplier>`
- `DbSet<Order>`
- `DbSet<Product>`
- `DbSet<Customer>`
- `DbSet<OrderItem>`
- `DbSet<TelephoneNumber>`

All entity relationships are already defined and must not be changed by Copilot.

Copilot should:
- Assume the DbContext is complete and correct
- Use it in examples
- Not scaffold or regenerate it unless explicitly asked

---

# 4. DTOs (Manually Created)

DTOs for all domain types are created manually.

DTO naming convention:
- `{EntityName}Dto`
- `Create{EntityName}Dto`
- `Update{EntityName}Dto`

Copilot must **not** generate DTOs unless explicitly asked.

---

# 5. Repositories (Manually Created)

Each domain type has a repository created manually.

Repository naming convention:
- `I{EntityName}Repository`
- `{EntityName}Repository`

Repository methods include:
- `GetByIdAsync`
- `GetAllAsync`
- `CreateAsync`
- `UpdateAsync`
- `DeleteAsync`
- `ExistsAsync`

Copilot should:
- Assume repositories exist
- Use them in examples
- Not generate or modify repository implementations unless explicitly asked

---

# 6. Application Services & Validators

Application services and validators are created manually and placed in the Infrastructure project.

## Validators

FluentValidation validators for DTOs:
- `Create{EntityName}DtoValidator`
- `Update{EntityName}DtoValidator`

Location: `Infrastructure/Validators/`

Copilot should:
- Assume validators exist
- Not generate validators unless explicitly asked
- Use them in examples for API validation

---

# 7. Messaging (RabbitMQ)

RabbitMQ is used as the message broker.

Local instance is run using Docker Desktop:

```bash
docker run -d --hostname rabbitmq-local --name rabbitmq \
  -p 5672:5672 -p 15672:15672 \
  rabbitmq:3-management
```

Management UI:
- http://localhost:15672
- Username: guest
- Password: guest

---

# 8. Publisher API (ASP.NET Core Web API)

The Publisher is now an **ASP.NET Core Web API** with RESTful controllers.

## Architecture

The Publisher API has the following structure:

```
Publisher/
├── Controllers/
│   ├── CustomerController.cs
│   ├── OrderController.cs
│   ├── ProductController.cs
│   ├── SupplierController.cs
│   └── TelephoneNumberController.cs
├── Services/
│   └── PublisherService.cs
├── Program.cs
└── appsettings.json
```

## Controller Endpoints

Each controller has the following endpoints:

### Customer Controller

- `POST /api/customers` - Create a new customer message
- `PUT /api/customers/{id}` - Update an existing customer message

### Order Controller

- `POST /api/orders` - Create a new order message
- `PUT /api/orders/{id}` - Update an existing order message

### Product Controller

- `POST /api/products` - Create a new product message
- `PUT /api/products/{id}` - Update an existing product message

### Supplier Controller

- `POST /api/suppliers` - Create a new supplier message
- `PUT /api/suppliers/{id}` - Update an existing supplier message

### TelephoneNumber Controller

- `POST /api/telephonenumbers` - Create a new telephone number message
- `PUT /api/telephonenumbers/{id}` - Update an existing telephone number message

## Message Format

When a controller endpoint is called, the Publisher sends a message to RabbitMQ in the following format:

```json
{
  "instruction": "create",
  "entityType": "Order",
  "payload": {
    ...entity data...
  }
}
```

or

```json
{
  "instruction": "update",
  "entityType": "Order",
  "payload": {
    ...entity data...
  }
}
```

## Request/Response

### Create Request

```json
POST /api/orders
Content-Type: application/json

{
  "supplierId": 1,
  "orderDate": "2025-02-18T00:00:00Z",
  "customerId": 123,
  "billingAddress": {
    "street": "123 Main St",
    "city": "Portland",
    "county": "OR",
    "postalCode": "97214",
    "country": "USA"
  }
}
```

### Update Request

```json
PUT /api/orders/{id}
Content-Type: application/json

{
  "supplierId": 1,
  "orderDate": "2025-02-18T00:00:00Z",
  "customerId": 123,
  "billingAddress": {
    "street": "123 Main St",
    "city": "Portland",
    "county": "OR",
    "postalCode": "97214",
    "country": "USA"
  }
}
```

## Copilot Responsibilities

Copilot should:

1. Generate controllers for each entity type (Customer, Order, Product, Supplier, TelephoneNumber)
2. Create action methods for CREATE (POST) and UPDATE (PUT) operations
3. Validate incoming DTOs using the FluentValidation validators
4. Use dependency injection to access the PublisherService
5. Call the PublisherService to send messages to RabbitMQ
6. Return appropriate HTTP status codes (200 OK, 201 Created, 400 Bad Request, etc.)
7. Not modify domain models, repositories, or validators

---

# 9. Subscriber Behavior

When generating subscriber examples, Copilot should:

- Deserialize the incoming message
- Inspect the Instruction field (create or update)
- Inspect the EntityType field to determine which repository to use
- Call the appropriate repository method (CreateAsync, UpdateAsync)
- Use dependency injection patterns common in .NET 8
- Not modify domain models or DbContext

---

# 10. Project-by-Project .csproj Requirements

## Domain.csproj

Contains only domain models and value objects.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.ComponentModel.Annotations" Version="*" />
  </ItemGroup>
</Project>
```

## Application.csproj

Contains DTOs, repository interfaces, and validators.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="*" />
  </ItemGroup>
</Project>
```

## Infrastructure.csproj

Contains EF Core DbContext, repository implementations, validators, and RabbitMQ helpers.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
    <ProjectReference Include="..\Application\Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.24" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.24" />
    <PackageReference Include="RabbitMQ.Client" Version="*" />
    <PackageReference Include="FluentValidation" Version="*" />
  </ItemGroup>
</Project>
```

## Publisher.csproj

ASP.NET Core Web API that publishes messages.

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
    <ProjectReference Include="..\Application\Application.csproj" />
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="*" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="*" />
    <PackageReference Include="RabbitMQ.Client" Version="*" />
    <PackageReference Include="FluentValidation.AspNetCore" Version="*" />
  </ItemGroup>
</Project>
```

## Subscriber.csproj

Console app or Worker Service that consumes messages.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
    <ProjectReference Include="..\Application\Application.csproj" />
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="RabbitMQ.Client" Version="*" />
    <PackageReference Include="System.Text.Json" Version="*" />
  </ItemGroup>
</Project>
```

---

# 11. Key Conventions

## Naming Conventions

- Controllers: `{EntityName}Controller`
- Services: `{EntityName}Service` or `PublisherService`
- DTOs: `{EntityName}Dto`, `Create{EntityName}Dto`, `Update{EntityName}Dto`
- Repositories: `I{EntityName}Repository`, `{EntityName}Repository`
- Validators: `{EntityName}DtoValidator`

## Dependency Injection

All services, repositories, and validators should use constructor injection in .NET 8 style:

```csharp
public class OrderController(IOrderRepository repository, PublisherService publisherService) : ControllerBase
{
    private readonly IOrderRepository _repository = repository;
    private readonly PublisherService _publisherService = publisherService;
}
```

## Code Style

- Use async/await for all I/O operations
- Use LINQ for queries
- Follow .NET 8 conventions and idioms
- Use nullable reference types (enabled in .csproj)
- Use PascalCase for public methods and properties
- Use camelCase for private fields and local variables

---

# 12. Important Reminders for Copilot

1. **Do NOT modify domain models** - They are created manually and should not be changed
2. **Do NOT generate DTOs or Validators** - They are created manually
3. **Do NOT modify the DbContext** - It is already complete
4. **Do NOT modify repository implementations** - They are manually created
5. **Do generate controllers** with POST and PUT actions when explicitly asked
6. **Do use dependency injection** following .NET 8 patterns
7. **Do validate requests** using FluentValidation validators
8. **Do send messages to RabbitMQ** through the PublisherService
9. **Do return appropriate HTTP status codes** in API responses
10. **Always use async/await** for all asynchronous operations
