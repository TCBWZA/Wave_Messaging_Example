using Domain.Models;

namespace Infrastructure.Repositories
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(long id, bool includeRelated = false);
        Task<Customer?> GetByEmailAsync(string email);
        Task<List<Customer>> GetAllAsync(bool includeRelated = false);
        Task<Customer> CreateAsync(Customer customer);
        Task<Customer> UpdateAsync(Customer customer);
        Task<bool> DeleteAsync(long id);
        Task<bool> ExistsAsync(long id);
        Task<bool> EmailExistsAsync(string email, long? excludeCustomerId = null);
        
        // Pagination support
        Task<(List<Customer> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool includeRelated = false);
        
        // Search and filtering
        Task<List<Customer>> SearchAsync(string? name, string? email);
        
        // Efficient read-only queries
        Task<List<Customer>> GetAllNoTrackingAsync();
    }

    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(long id);
        Task<Product?> GetByProductCodeAsync(Guid productCode);
        Task<List<Product>> GetAllAsync();
        Task<(List<Product> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
        Task<List<Product>> SearchAsync(string? name, Guid? productCode);
        Task<Product> CreateAsync(Product product);
        Task<Product> UpdateAsync(Product product);
        Task<bool> DeleteAsync(long id);
        Task<bool> ExistsAsync(long id);
        Task<bool> ProductCodeExistsAsync(Guid productCode, long? excludeProductId = null);
    }

    public interface ITelephoneNumberRepository
    {
        Task<TelephoneNumber?> GetByIdAsync(long id);
        Task<List<TelephoneNumber>> GetAllAsync();
        Task<List<TelephoneNumber>> GetByCustomerIdAsync(long customerId);
        Task<TelephoneNumber> CreateAsync(TelephoneNumber telephoneNumber);
        Task<TelephoneNumber> UpdateAsync(TelephoneNumber telephoneNumber);
        Task<bool> DeleteAsync(long id);
        Task<bool> ExistsAsync(long id);
    }

    public interface ISupplierRepository
    {
        Task<Supplier?> GetByIdAsync(long id);
        Task<Supplier?> GetByNameAsync(string name);
        Task<List<Supplier>> GetAllAsync();
        Task<Supplier> CreateAsync(Supplier supplier);
        Task<Supplier> UpdateAsync(Supplier supplier);
        Task<bool> DeleteAsync(long id);
        Task<bool> ExistsAsync(long id);
        Task<bool> NameExistsAsync(string name, long? excludeSupplierId = null);
    }

    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(long id, bool includeRelated = false);
        Task<List<Order>> GetAllAsync(bool includeRelated = false);
        Task<List<Order>> GetByCustomerIdAsync(long customerId, bool includeRelated = false);
        Task<List<Order>> GetBySupplierIdAsync(long supplierId, bool includeRelated = false);
        Task<(List<Order> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool includeRelated = false);
        Task<Order> CreateAsync(Order order);
        Task<Order> UpdateAsync(Order order);
        Task<bool> DeleteAsync(long id);
        Task<bool> ExistsAsync(long id);
    }
}
