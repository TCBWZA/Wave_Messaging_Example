using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class TelephoneNumberDto
    {
        public long Id { get; set; }

        public long CustomerId { get; set; }

        [RegularExpression("^(Mobile|Work|DirectDial)$", ErrorMessage = "Type must be Mobile, Work, or DirectDial.")]
        [StringLength(20, ErrorMessage = "Type cannot exceed 20 characters.")]
        public string? Type { get; set; }

        [StringLength(50, ErrorMessage = "Number cannot exceed 50 characters.")]
        public string? Number { get; set; }
    }

    public class CreateTelephoneNumberDto
    {
        [Required(ErrorMessage = "CustomerId is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "CustomerId must be greater than 0.")]
        public long CustomerId { get; set; }

        [Required(ErrorMessage = "Type is required.")]
        [RegularExpression("^(Mobile|Work|DirectDial)$", ErrorMessage = "Type must be Mobile, Work, or DirectDial.")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Number is required.")]
        [StringLength(50, ErrorMessage = "Number cannot exceed 50 characters.")]
        public string Number { get; set; } = string.Empty;
    }

    public class UpdateTelephoneNumberDto
    {
        [Required(ErrorMessage = "Type is required.")]
        [RegularExpression("^(Mobile|Work|DirectDial)$", ErrorMessage = "Type must be Mobile, Work, or DirectDial.")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Number is required.")]
        [StringLength(50, ErrorMessage = "Number cannot exceed 50 characters.")]
        public string Number { get; set; } = string.Empty;
    }
}
