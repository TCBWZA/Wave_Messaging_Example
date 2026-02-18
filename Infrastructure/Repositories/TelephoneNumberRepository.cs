using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Repositories
{
    public class TelephoneNumberRepository : ITelephoneNumberRepository
    {
        private readonly AppDbContext _context;

        public TelephoneNumberRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TelephoneNumber?> GetByIdAsync(long id)
        {
            return await _context.TelephoneNumbers.FindAsync(id);
        }

        public async Task<List<TelephoneNumber>> GetAllAsync()
        {
            return await _context.TelephoneNumbers.ToListAsync();
        }

        public async Task<List<TelephoneNumber>> GetByCustomerIdAsync(long customerId)
        {
            return await _context.TelephoneNumbers
                .Where(t => t.CustomerId == customerId)
                .ToListAsync();
        }

        public async Task<TelephoneNumber> CreateAsync(TelephoneNumber telephoneNumber)
        {
            _context.TelephoneNumbers.Add(telephoneNumber);
            await _context.SaveChangesAsync();
            return telephoneNumber;
        }

        public async Task<TelephoneNumber> UpdateAsync(TelephoneNumber telephoneNumber)
        {
            _context.TelephoneNumbers.Update(telephoneNumber);
            await _context.SaveChangesAsync();
            return telephoneNumber;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var telephoneNumber = await _context.TelephoneNumbers.FindAsync(id);
            if (telephoneNumber == null)
                return false;

            _context.TelephoneNumbers.Remove(telephoneNumber);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.TelephoneNumbers.AnyAsync(t => t.Id == id);
        }
    }
}
