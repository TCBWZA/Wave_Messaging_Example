using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class SupplierDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CreateSupplierDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateSupplierDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;
    }
}
