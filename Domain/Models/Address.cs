using System.ComponentModel.DataAnnotations;

namespace Domain.Models
{
    public class Address
    {
        /// <summary>
        /// Street address line 1 (required to identify the address exists).
        /// </summary>
        [Required(ErrorMessage = "Street is required.")]
        [StringLength(200, ErrorMessage = "Street cannot exceed 200 characters.")]
        public string Street { get; set; } = string.Empty;

        /// <summary>
        /// City name.
        /// </summary>
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        /// <summary>
        /// County/State.
        /// </summary>
        [StringLength(100, ErrorMessage = "County cannot exceed 100 characters.")]
        public string? County { get; set; }

        /// <summary>
        /// Postal/ZIP code.
        /// </summary>
        [StringLength(20, ErrorMessage = "PostalCode cannot exceed 20 characters.")]
        public string? PostalCode { get; set; }

        /// <summary>
        /// Country name.
        /// </summary>
        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters.")]
        public string? Country { get; set; }
    }
}
