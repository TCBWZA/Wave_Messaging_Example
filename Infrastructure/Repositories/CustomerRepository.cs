using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Repositories
{
    public class CustomerRepository(AppDbContext context) : ICustomerRepository
    {
        private readonly AppDbContext _context = context;

        public async Task<Customer?> GetByIdAsync(long id, bool includeRelated = false)
        {
            var query = _context.Customers.AsQueryable();

            if (includeRelated)
            {
                query = query
                    .Include(c => c.PhoneNumbers);
            }

            //return await query.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            // TUTOR NOTE: Intentional Error - This method does not handle null correctly.
            // It will track entities in memory unnecessarily, causing potential issues with data consistency.
            // Students should write tests that verify: 1) Null is handled correctly, 2) AsNoTracking is used for read-only operations.
            return await query.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Customer?> GetByEmailAsync(string email)
        {
            return await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == email);
        }

        public async Task<List<Customer>> GetAllAsync(bool includeRelated = false)
        {
            var query = _context.Customers.AsQueryable();

            if (includeRelated)
            {
                query = query
                    .AsSplitQuery()
                    .Include(c => c.PhoneNumbers);
            }

            return await query.AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Implements pagination using Skip() and Take().
        /// IMPORTANT: Always use OrderBy() before Skip/Take to ensure consistent results.
        /// Returns both the current page of items and the total count for pagination metadata.
        /// 
        /// How it works:
        /// - Skip((page - 1) * pageSize): Skips items from previous pages
        /// - Take(pageSize): Returns only the requested number of items
        /// - CountAsync(): Gets total count for calculating total pages
        /// 
        /// Example: page=2, pageSize=10
        /// - Skip(10) => Skip first 10 items (page 1)
        /// - Take(10) => Return next 10 items (page 2)
        /// </summary>
        public async Task<(List<Customer> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            bool includeRelated = false)
        {
            var query = _context.Customers.AsQueryable();

            if (includeRelated)
            {
                query = query
                    .Include(c => c.PhoneNumbers);
            }

            var totalCount = await query.AsNoTracking().CountAsync();
            if (totalCount > 0)
            {
                var items = await query
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (items, totalCount);
            }
            return (new List<Customer>(), 0);
        }
        /// <summary>
        /// Demonstrates dynamic filtering with multiple optional parameters.
        /// Each filter is only applied if the parameter has a value.
        /// This allows flexible search combinations without creating multiple methods.
        /// 
        /// Query building pattern:
        /// 1. Start with AsQueryable() to get IQueryable<Customer>
        /// 2. Chain Where() clauses conditionally
        /// 3. Execute with ToListAsync() only at the end
        /// 
        /// SQL is not executed until ToListAsync() is called, so all WHERE clauses
        /// are combined into a single efficient SQL query.
        /// 
        /// Example filters:
        /// - name: Uses LIKE operator (Contains => SQL LIKE '%value%')
        /// - email: Case-insensitive search in most databases
        /// </summary>
        public async Task<List<Customer>> SearchAsync(
            string? name,
            string? email)
        {
            // Start with base query - no SQL executed yet
            var query = _context.Customers.AsQueryable();

            // Conditionally add WHERE clauses
            // SQL: WHERE Name LIKE '%value%'
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(c => c.Name!.Contains(name));
            }

            // SQL: WHERE Email LIKE '%value%'
            if (!string.IsNullOrEmpty(email))
            {
                query = query.Where(c => c.Email!.Contains(email));
            }

            // NOW execute the query with all WHERE clauses combined
            return await query.AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Demonstrates AsNoTracking() for read-only queries.
        /// 
        /// When should you use AsNoTracking()?
        /// - READ-ONLY scenarios (viewing data, reports, exports)
        /// - You don't need to update the entities
        /// - You want better performance
        /// 
        /// Performance benefits:
        /// - No change tracking overhead (EF doesn't monitor entity changes)
        /// - Lower memory usage (no tracking snapshots)
        /// - Faster query execution (10-30% faster for large datasets)
        /// 
        /// WARNING: Do NOT use AsNoTracking() if you plan to:
        /// - Update the entities
        /// - Save changes back to database
        /// - Use the entities in Update/Delete operations
        /// 
        /// The entities returned are "disconnected" from the DbContext.
        /// </summary>
        public async Task<List<Customer>> GetAllNoTrackingAsync()
        {
            return await _context.Customers
                .AsNoTracking()  // Disables change tracking for performance
                .Include(c => c.PhoneNumbers)
                .ToListAsync();
        }

        /// <summary>
        /// Creates a new customer and saves any related telephone numbers.
        /// 
        /// EF Core behavior with related entities:
        /// - When you Add() a parent entity (Customer), EF Core automatically tracks
        ///   any related entities in navigation properties (PhoneNumbers)
        /// - All tracked entities are saved in a single transaction when SaveChangesAsync() is called
        /// - Foreign keys are automatically set (CustomerId in TelephoneNumber)
        /// 
        /// What gets saved:
        /// 1. The Customer entity to the Customers table
        /// 2. Any TelephoneNumber entities in customer.PhoneNumbers to the TelephoneNumbers table
        /// 
        /// Example usage:
        /// var customer = new Customer 
        /// { 
        ///     Name = "Acme Corp",
        ///     Email = "contact@acme.com",
        ///     PhoneNumbers = new List<TelephoneNumber> 
        ///     {
        ///         new TelephoneNumber { Type = "Mobile", Number = "555-1234" },
        ///         new TelephoneNumber { Type = "Work", Number = "555-5678" }
        ///     }
        /// };
        /// var saved = await CreateAsync(customer);
        /// // All entities are saved atomically in a single database transaction
        /// // saved.Id will contain the generated customer ID
        /// // saved.PhoneNumbers[0].CustomerId will be automatically set
        /// </summary>
        public async Task<Customer> CreateAsync(Customer customer)
        {
            // Add customer to context - EF Core automatically tracks related entities
            // in customer.PhoneNumbers collection
            _context.Customers.Add(customer);

            // SaveChangesAsync executes in a transaction and saves:
            // 1. Customer to Customers table
            // 2. Related TelephoneNumber entities to TelephoneNumbers table (if any)
            // Foreign keys (CustomerId) are automatically populated by EF Core
            await _context.SaveChangesAsync();

            return customer;
        }

        public async Task<Customer> UpdateAsync(Customer customer)
        {
            // TUTOR NOTE: Intentional Error - This method doesn't validate input before updating.
            // No null check for customer parameter. No check if customer actually exists.
            // Students should write tests to verify: 1) Null input throws ArgumentNullException, 2) Non-existent customer throws KeyNotFoundException
            // Currently the commented code shows how it SHOULD be done with proper exception handling.
            //try
            //{
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
                return customer;
            //}
            //catch (DbUpdateConcurrencyException)
            //{
            //    if (!await ExistsAsync(customer.Id))
            //    {
            //        throw new KeyNotFoundException($"Customer with ID {customer.Id} not found.");
            //    }
            //    else
            //    {
            //        throw;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    throw new Exception("An error occurred while updating the customer.", ex.InnerException);
            //}

        }

        public async Task<bool> DeleteAsync(long id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return false;

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.Customers.AnyAsync(c => c.Id == id);
        }

        public async Task<bool> EmailExistsAsync(string email, long? excludeCustomerId = null)
        {
            var query = _context.Customers.Where(c => c.Email == email);

            if (excludeCustomerId.HasValue)
            {
                query = query.Where(c => c.Id != excludeCustomerId.Value);
            }

            return await query.AsNoTracking().AnyAsync();
        }
    }
}
