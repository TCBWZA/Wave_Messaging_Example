using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CustomerDto
    {
        public long Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters.")]
        public string Email { get; set; } = string.Empty;

        public List<TelephoneNumberDto>? PhoneNumbers { get; set; }
    }

    public class CreateCustomerDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters.")]
        public string Email { get; set; } = string.Empty;

        public List<CreateTelephoneNumberForCustomerDto>? PhoneNumbers { get; set; }
    }

    public class CreateTelephoneNumberForCustomerDto
    {
        [RegularExpression("^(Mobile|Work|DirectDial)$", ErrorMessage = "Type must be Mobile, Work, or DirectDial.")]
        public string? Type { get; set; }

        [StringLength(50, ErrorMessage = "Number cannot exceed 50 characters.")]
        public string? Number { get; set; }
    }

    public class UpdateCustomerDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters.")]
        public string Email { get; set; } = string.Empty;
    }
}
