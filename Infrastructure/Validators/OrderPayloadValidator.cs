using System.Text.Json;
using FluentValidation;

namespace Infrastructure.Validators
{
    /// <summary>
    /// Validator for order payloads received from RabbitMQ.
    /// Used by the Subscriber to validate JSON messages before database operations.
    /// </summary>
    public class OrderPayloadValidator
    {
        public List<string> Validate(JsonElement payload)
        {
            var errors = new List<string>();

            // Validate required fields exist
            if (!payload.TryGetProperty("customerId", out var customerIdElement) || customerIdElement.ValueKind == JsonValueKind.Null)
                errors.Add("Order payload missing required field: 'customerId'");

            if (!payload.TryGetProperty("supplierId", out var supplierIdElement) || supplierIdElement.ValueKind == JsonValueKind.Null)
                errors.Add("Order payload missing required field: 'supplierId'");

            if (!payload.TryGetProperty("orderDate", out var orderDateElement) || orderDateElement.ValueKind == JsonValueKind.Null)
                errors.Add("Order payload missing required field: 'orderDate'");

            if (!payload.TryGetProperty("customerEmail", out var customerEmailElement) || customerEmailElement.ValueKind == JsonValueKind.Null)
                errors.Add("Order payload missing required field: 'customerEmail'");

            if (!payload.TryGetProperty("orderStatus", out var orderStatusElement) || orderStatusElement.ValueKind == JsonValueKind.Null)
                errors.Add("Order payload missing required field: 'orderStatus'");

            if (!payload.TryGetProperty("billingAddress", out var billingAddressElement) || billingAddressElement.ValueKind == JsonValueKind.Null)
                errors.Add("Order payload missing required field: 'billingAddress'");

            // If we have basic field errors, return early
            if (errors.Any())
                return errors;

            // Validate field values
            if (customerIdElement.ValueKind != JsonValueKind.Null)
            {
                var customerId = customerIdElement.GetInt64();
                if (customerId <= 0)
                    errors.Add("Order 'customerId' must be greater than 0");
            }

            if (supplierIdElement.ValueKind != JsonValueKind.Null)
            {
                var supplierId = supplierIdElement.GetInt64();
                if (supplierId <= 0)
                    errors.Add("Order 'supplierId' must be greater than 0");
            }

            if (customerEmailElement.ValueKind != JsonValueKind.Null)
            {
                var customerEmail = customerEmailElement.GetString();
                if (string.IsNullOrWhiteSpace(customerEmail))
                    errors.Add("Order 'customerEmail' cannot be empty");
                else if (!customerEmail.Contains("@"))
                    errors.Add("Order 'customerEmail' is not in valid email format");
                else if (customerEmail.Length > 200)
                    errors.Add("Order 'customerEmail' exceeds maximum length of 200 characters");
            }

            if (orderStatusElement.ValueKind != JsonValueKind.Null)
            {
                var orderStatus = orderStatusElement.GetString();
                if (string.IsNullOrWhiteSpace(orderStatus))
                    errors.Add("Order 'orderStatus' cannot be empty");
                else if (!Enum.TryParse<Domain.Models.OrderStatus>(orderStatus, true, out _))
                    errors.Add($"Order 'orderStatus' has invalid value: {orderStatus}");
            }

            // Validate billing address
            ValidateAddress(billingAddressElement, "billingAddress", errors);

            // Validate optional delivery address
            if (payload.TryGetProperty("deliveryAddress", out var deliveryAddressElement) && deliveryAddressElement.ValueKind != JsonValueKind.Null)
            {
                ValidateAddress(deliveryAddressElement, "deliveryAddress", errors);
            }

            // Validate order items if present
            if (payload.TryGetProperty("orderItems", out var orderItemsElement) && orderItemsElement.ValueKind != JsonValueKind.Null)
            {
                ValidateOrderItems(orderItemsElement, errors);
            }

            return errors;
        }

        private void ValidateAddress(JsonElement addressElement, string addressFieldName, List<string> errors)
        {
            if (addressElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"Order '{addressFieldName}' must be an object");
                return;
            }

            if (!addressElement.TryGetProperty("street", out var streetElement) || streetElement.ValueKind == JsonValueKind.Null)
            {
                errors.Add($"Order '{addressFieldName}.street' is required");
                return;
            }

            var street = streetElement.GetString();
            if (string.IsNullOrWhiteSpace(street))
                errors.Add($"Order '{addressFieldName}.street' cannot be empty");
            else if (street.Length > 200)
                errors.Add($"Order '{addressFieldName}.street' exceeds maximum length of 200 characters");

            // Validate optional address fields
            if (addressElement.TryGetProperty("city", out var cityElement) && cityElement.ValueKind != JsonValueKind.Null)
            {
                var city = cityElement.GetString();
                if (city?.Length > 100)
                    errors.Add($"Order '{addressFieldName}.city' exceeds maximum length of 100 characters");
            }

            if (addressElement.TryGetProperty("county", out var countyElement) && countyElement.ValueKind != JsonValueKind.Null)
            {
                var county = countyElement.GetString();
                if (county?.Length > 100)
                    errors.Add($"Order '{addressFieldName}.county' exceeds maximum length of 100 characters");
            }

            if (addressElement.TryGetProperty("postalCode", out var postalCodeElement) && postalCodeElement.ValueKind != JsonValueKind.Null)
            {
                var postalCode = postalCodeElement.GetString();
                if (postalCode?.Length > 20)
                    errors.Add($"Order '{addressFieldName}.postalCode' exceeds maximum length of 20 characters");
            }

            if (addressElement.TryGetProperty("country", out var countryElement) && countryElement.ValueKind != JsonValueKind.Null)
            {
                var country = countryElement.GetString();
                if (country?.Length > 100)
                    errors.Add($"Order '{addressFieldName}.country' exceeds maximum length of 100 characters");
            }
        }

        private void ValidateOrderItems(JsonElement orderItemsElement, List<string> errors)
        {
            if (orderItemsElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add("Order 'orderItems' must be an array");
                return;
            }

            var itemIndex = 0;
            foreach (var itemElement in orderItemsElement.EnumerateArray())
            {
                if (!itemElement.TryGetProperty("productId", out var productIdElement) || productIdElement.ValueKind == JsonValueKind.Null)
                {
                    errors.Add($"Order item {itemIndex} missing required field: 'productId'");
                    itemIndex++;
                    continue;
                }

                if (!itemElement.TryGetProperty("quantity", out var quantityElement) || quantityElement.ValueKind == JsonValueKind.Null)
                {
                    errors.Add($"Order item {itemIndex} missing required field: 'quantity'");
                    itemIndex++;
                    continue;
                }

                if (!itemElement.TryGetProperty("unitPrice", out var unitPriceElement) || unitPriceElement.ValueKind == JsonValueKind.Null)
                {
                    errors.Add($"Order item {itemIndex} missing required field: 'unitPrice'");
                    itemIndex++;
                    continue;
                }

                var productId = productIdElement.GetInt64();
                var quantity = quantityElement.GetInt32();
                var unitPrice = unitPriceElement.GetDecimal();

                if (productId <= 0)
                    errors.Add($"Order item {itemIndex} 'productId' must be greater than 0");

                if (quantity <= 0)
                    errors.Add($"Order item {itemIndex} 'quantity' must be greater than 0");

                if (unitPrice < 0)
                    errors.Add($"Order item {itemIndex} 'unitPrice' cannot be negative");

                itemIndex++;
            }
        }
    }
}
