using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Repositories
{
    public class OrderRepository(AppDbContext context) : IOrderRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<Order?> GetByIdAsync(long id, bool includeRelated = false)
        {
            var query = _context.Orders.AsQueryable();

            if (includeRelated)
            {
                query = query
                    .Include(o => o.Supplier)
                    .Include(o => o.OrderItems!)
                        .ThenInclude(oi => oi.Product);
            }

            return await query.FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Order>> GetAllAsync(bool includeRelated = false)
        {
            var query = _context.Orders.AsQueryable();

            if (includeRelated)
            {
                query = query
                    .AsSplitQuery()
                    .Include(o => o.Supplier)
                    .Include(o => o.OrderItems!)
                        .ThenInclude(oi => oi.Product);
            }

            return await query.AsNoTracking().OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public async Task<List<Order>> GetByCustomerIdAsync(long customerId, bool includeRelated = false)
        {
            var query = _context.Orders.Where(o => o.CustomerId == customerId);

            if (includeRelated)
            {
                query = query
                    .Include(o => o.Supplier)
                    .Include(o => o.OrderItems!)
                        .ThenInclude(oi => oi.Product);
            }

            return await query.AsNoTracking().OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public async Task<List<Order>> GetBySupplierIdAsync(long supplierId, bool includeRelated = false)
        {
            var query = _context.Orders.Where(o => o.SupplierId == supplierId);

            if (includeRelated)
            {
                query = query
                    .Include(o => o.Supplier)
                    .Include(o => o.OrderItems!)
                        .ThenInclude(oi => oi.Product);
            }

            return await query.AsNoTracking().OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public async Task<(List<Order> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, bool includeRelated = false)
        {
            var query = _context.Orders.AsQueryable();

            if (includeRelated)
            {
                query = query
                    .Include(o => o.Supplier)
                    .Include(o => o.OrderItems!)
                        .ThenInclude(oi => oi.Product);
            }

            var totalCount = await query.CountAsync();

            if (totalCount > 0)
            {
                var items = await query
                    .AsNoTracking()
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (items, totalCount);
            }

            return (new List<Order>(), 0);
        }

        public async Task<Order> CreateAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order> UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return false;

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.Orders.AnyAsync(o => o.Id == id);
        }
    }
}
