using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Repositories
{
    public class SupplierRepository(AppDbContext context) : ISupplierRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<Supplier?> GetByIdAsync(long id)
        {
            return await _context.Suppliers.FindAsync(id);
        }

        public async Task<Supplier?> GetByNameAsync(string name)
        {
            return await _context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Name == name);
        }

        public async Task<List<Supplier>> GetAllAsync()
        {
            return await _context.Suppliers
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Supplier> CreateAsync(Supplier supplier)
        {
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return supplier;
        }

        public async Task<Supplier> UpdateAsync(Supplier supplier)
        {
            _context.Suppliers.Update(supplier);
            await _context.SaveChangesAsync();
            return supplier;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
                return false;

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.Suppliers.AnyAsync(s => s.Id == id);
        }

        public async Task<bool> NameExistsAsync(string name, long? excludeSupplierId = null)
        {
            var query = _context.Suppliers.Where(s => s.Name == name);

            if (excludeSupplierId.HasValue)
            {
                query = query.Where(s => s.Id != excludeSupplierId.Value);
            }

            return await query.AsNoTracking().AnyAsync();
        }
    }
}
