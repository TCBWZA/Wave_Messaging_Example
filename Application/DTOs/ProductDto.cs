using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class ProductDto
    {
        public long Id { get; set; }
        public Guid ProductCode { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CreateProductDto
    {
        [Required(ErrorMessage = "ProductCode is required.")]
        public Guid ProductCode { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateProductDto
    {
        [Required(ErrorMessage = "ProductCode is required.")]
        public Guid ProductCode { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}
