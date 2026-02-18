# Publisher API - Swagger UI Documentation

## Overview

The Publisher API is an ASP.NET Core Web API that provides RESTful endpoints for publishing domain events to RabbitMQ. The API includes comprehensive Swagger UI documentation that allows you to explore, test, and understand all available endpoints.

## Accessing Swagger UI

Once the application is running, open your browser and navigate to:

```
https://localhost:7000/
```

or

```
https://localhost:5001/
```

(The exact port depends on your local configuration. Check the console output when the app starts.)

## Swagger UI Features

### 1. **API Documentation**
- **Endpoint Descriptions**: Each endpoint shows a detailed description of what it does
- **Request/Response Examples**: View the expected input and output formats
- **Status Codes**: See all possible HTTP response codes for each operation
- **Authentication Requirements**: (if applicable)

### 2. **Try It Out**
Swagger UI allows you to test API endpoints directly from the browser:

1. Click on any endpoint (e.g., `POST /api/customers`)
2. Click the **"Try it out"** button
3. Fill in the required parameters and request body
4. Click **"Execute"**
5. View the response and response code

### 3. **Model Schemas**
Scroll down to see the **Schemas** section which displays:
- All DTO models (CustomerDto, CreateCustomerDto, UpdateCustomerDto, etc.)
- Required fields and data types
- Field descriptions and constraints

## Available Endpoints

### Customers
- `POST /api/customers` - Create a new customer
- `PUT /api/customers/{id}` - Update an existing customer

### Orders
- `POST /api/orders` - Create a new order
- `PUT /api/orders/{id}` - Update an existing order

### Products
- `POST /api/products` - Create a new product
- `PUT /api/products/{id}` - Update an existing product

### Suppliers
- `POST /api/suppliers` - Create a new supplier
- `PUT /api/suppliers/{id}` - Update an existing supplier

### Telephone Numbers
- `POST /api/telephonenumbers` - Create a new telephone number
- `PUT /api/telephonenumbers/{id}` - Update a telephone number

## Example Request

### Create a Customer

1. Navigate to Swagger UI: `https://localhost:7000/`
2. Find the **Customers** section
3. Click on `POST /api/customers`
4. Click **"Try it out"**
5. Replace the example JSON with:

```json
{
  "name": "Acme Corporation",
  "email": "contact@acme.com",
  "phoneNumbers": [
    {
      "type": "Work",
      "number": "+1-555-0123"
    }
  ]
}
```

6. Click **"Execute"**
7. You'll see a `201 Created` response with the created customer

## Example Request - Create an Order

1. Find the **Orders** section
2. Click on `POST /api/orders`
3. Click **"Try it out"**
4. Replace the example JSON with:

```json
{
  "supplierId": 1,
  "orderDate": "2025-02-18T10:30:00Z",
  "customerId": 1,
  "customerEmail": "customer@example.com",
  "billingAddress": {
    "street": "123 Main St",
    "city": "Portland",
    "county": "OR",
    "postalCode": "97214",
    "country": "USA"
  },
  "orderStatus": "Received",
  "orderItems": [
    {
      "productId": 1,
      "quantity": 5,
      "price": 99.99
    }
  ]
}
```

5. Click **"Execute"**
6. View the `201 Created` response

## Response Codes

| Status Code | Meaning |
|-------------|---------|
| `201 Created` | Resource was successfully created |
| `200 OK` | Request was successful |
| `400 Bad Request` | Invalid input or validation error |
| `404 Not Found` | Resource not found |
| `500 Internal Server Error` | Server error |

## RabbitMQ Integration

When you create or update an entity through the API:

1. **Database**: The entity is saved to the SQL Server database
2. **RabbitMQ**: A message is published to the `messaging_exchange` exchange
3. **Response**: The API returns the created/updated entity with the appropriate HTTP status code

Example message published to RabbitMQ:

```json
{
  "instruction": "create",
  "entityType": "Customer",
  "payload": {
    "id": 1,
    "name": "Acme Corporation",
    "email": "contact@acme.com"
  }
}
```

## Troubleshooting

### Swagger UI Not Loading
- Ensure the application is running: Check that you see "Application started" in the console
- Try the exact port shown in the console output
- Clear your browser cache and refresh

### "Connection refused" error
- Verify RabbitMQ is running: `docker ps | grep rabbitmq`
- Start RabbitMQ if needed: `docker run -d --hostname rabbitmq-local --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management`
- Check SQL Server connection string in `appsettings.json`

### Database errors
- Ensure SQL Server is running and accessible
- Run Entity Framework migrations if needed
- Check the connection string in `appsettings.json`

## API Security

Currently, the API has no authentication enabled. For production deployment:

1. Add JWT authentication
2. Add authorization policies
3. Add HTTPS enforcement
4. Enable CORS restrictions
5. Rate limiting

## Additional Resources

- [Swagger/OpenAPI Specification](https://swagger.io/)
- [Swashbuckle Documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [ASP.NET Core Web API Documentation](https://docs.microsoft.com/en-us/aspnet/core/web-api)
- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
