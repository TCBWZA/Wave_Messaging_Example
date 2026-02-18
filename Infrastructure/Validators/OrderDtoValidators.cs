using FluentValidation;
using Application.DTOs;

namespace Infrastructure.Validators
{
    /// <summary>
    /// FluentValidation validator for CreateOrderDto.
    /// Ensures that either CustomerId or CustomerEmail is provided (not both null).
    /// </summary>
    public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
    {
        public CreateOrderDtoValidator()
        {
            // Custom rule: Either CustomerId or CustomerEmail must be provided
            RuleFor(x => x)
                .Must(dto => dto.CustomerId.HasValue || !string.IsNullOrWhiteSpace(dto.CustomerEmail))
                .WithMessage("Either CustomerId or CustomerEmail must be provided.")
                .OverridePropertyName("Order");

            // When CustomerId is provided, it must be greater than 0
            When(x => x.CustomerId.HasValue, () =>
            {
                RuleFor(x => x.CustomerId!.Value)
                    .GreaterThan(0)
                    .WithMessage("CustomerId must be greater than zero when provided.");
            });

            // When CustomerEmail is provided, it must be a valid email
            When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail), () =>
            {
                RuleFor(x => x.CustomerEmail)
                    .EmailAddress()
                    .WithMessage("CustomerEmail must be a valid email address when provided.")
                    .MaximumLength(200)
                    .WithMessage("CustomerEmail cannot exceed 200 characters.");
            });

            // SupplierId validation
            RuleFor(x => x.SupplierId)
                .GreaterThan(0)
                .WithMessage("SupplierId must be greater than zero.");

            // OrderDate validation
            RuleFor(x => x.OrderDate)
                .NotEmpty()
                .WithMessage("OrderDate is required.");

            // OrderStatus validation
            RuleFor(x => x.OrderStatus)
                .IsInEnum()
                .WithMessage("OrderStatus must be a valid status.");

            // TUTOR NOTE: Intentional Error - OrderItems validation is incomplete.
            // NotEmpty() validates the collection but doesn't validate individual items.
            // There's no validation for quantity (should be > 0) or price (should be >= 0).
            // Additionally, Product existence is not checked.
            // Students should write tests to verify: 1) Each item has quantity > 0, 2) Each item has price >= 0, 3) Products exist.
            RuleFor(x => x.OrderItems)
                .NotEmpty()
                .WithMessage("Order must contain at least one item.");
        }
    }

    /// <summary>
    /// FluentValidation validator for UpdateOrderDto.
    /// Ensures that either CustomerId or CustomerEmail is provided (not both null).
    /// </summary>
    public class UpdateOrderDtoValidator : AbstractValidator<UpdateOrderDto>
    {
        public UpdateOrderDtoValidator()
        {
            // Custom rule: Either CustomerId or CustomerEmail must be provided
            RuleFor(x => x)
                .Must(dto => dto.CustomerId.HasValue || !string.IsNullOrWhiteSpace(dto.CustomerEmail))
                .WithMessage("Either CustomerId or CustomerEmail must be provided.")
                .OverridePropertyName("Order");

            // When CustomerId is provided, it must be greater than 0
            When(x => x.CustomerId.HasValue, () =>
            {
                RuleFor(x => x.CustomerId!.Value)
                    .GreaterThan(0)
                    .WithMessage("CustomerId must be greater than zero when provided.");
            });

            // When CustomerEmail is provided, it must be a valid email
            When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail), () =>
            {
                RuleFor(x => x.CustomerEmail)
                    .EmailAddress()
                    .WithMessage("CustomerEmail must be a valid email address when provided.")
                    .MaximumLength(200)
                    .WithMessage("CustomerEmail cannot exceed 200 characters.");
            });

            // SupplierId validation
            RuleFor(x => x.SupplierId)
                .GreaterThan(0)
                .WithMessage("SupplierId must be greater than zero.");

            // OrderDate validation
            RuleFor(x => x.OrderDate)
                .NotEmpty()
                .WithMessage("OrderDate is required.");

            // OrderStatus validation
            RuleFor(x => x.OrderStatus)
                .IsInEnum()
                .WithMessage("OrderStatus must be a valid status.");

            // OrderItems validation
            RuleFor(x => x.OrderItems)
                .NotEmpty()
                .WithMessage("Order must contain at least one item.");
        }
    }
}
