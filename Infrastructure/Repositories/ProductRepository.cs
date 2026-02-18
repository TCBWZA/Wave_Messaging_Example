using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Repositories
{
    public class ProductRepository(AppDbContext context) : IProductRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<Product?> GetByIdAsync(long id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product?> GetByProductCodeAsync(Guid productCode)
        {
            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductCode == productCode);
        }

        public async Task<List<Product>> GetAllAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
        {
            // TUTOR NOTE: Intentional Error - No validation for page and pageSize parameters.
            // If page = 0 or pageSize = 0, this will cause incorrect Skip/Take behavior or unexpected results.
            // Students should write tests to verify that: 1) page must be >= 1, 2) pageSize must be > 0, 3) Exceptions are thrown for invalid values.
            var totalCount = await _context.Products.CountAsync();

            if (totalCount > 0)
            {
                var items = await _context.Products
                    .AsNoTracking()
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (items, totalCount);
            }

            return (new List<Product>(), 0);
        }

        public async Task<List<Product>> SearchAsync(string? name, Guid? productCode)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(p => p.Name.Contains(name));
            }

            if (productCode.HasValue)
            {
                query = query.Where(p => p.ProductCode == productCode.Value);
            }

            return await query.AsNoTracking().ToListAsync();
        }

        public async Task<Product> CreateAsync(Product product)
        {
            // TUTOR NOTE: Intentional Error - No null check for the product parameter.
            // This will cause a NullReferenceException if null is passed.
            // Students should write tests that verify null input is handled gracefully with proper exception throwing.
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product), "Product cannot be null.");
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product> UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.Products.AnyAsync(p => p.Id == id);
        }

        public async Task<bool> ProductCodeExistsAsync(Guid productCode, long? excludeProductId = null)
        {
            var query = _context.Products.Where(p => p.ProductCode == productCode);

            if (excludeProductId.HasValue)
            {
                query = query.Where(p => p.Id != excludeProductId.Value);
            }

            return await query.AsNoTracking().AnyAsync();
        }
    }
}
