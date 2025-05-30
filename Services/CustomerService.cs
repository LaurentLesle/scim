using Microsoft.EntityFrameworkCore;
using ScimServiceProvider.Data;
using ScimServiceProvider.Models;

namespace ScimServiceProvider.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ScimDbContext _context;

        public CustomerService(ScimDbContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetCustomerAsync(string id)
        {
            return await _context.Customers.FindAsync(id);
        }

        public async Task<Customer?> GetCustomerByTenantIdAsync(string tenantId)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.TenantId == tenantId);
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _context.Customers.ToListAsync();
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            // Ensure we have an ID
            if (string.IsNullOrEmpty(customer.Id))
            {
                customer.Id = Guid.NewGuid().ToString();
            }
            
            customer.Created = DateTime.UtcNow;
            customer.LastModified = DateTime.UtcNow;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return customer;
        }

        public async Task<Customer?> UpdateCustomerAsync(string id, Customer customer)
        {
            var existingCustomer = await _context.Customers.FindAsync(id);
            if (existingCustomer == null)
                return null;

            existingCustomer.Name = customer.Name;
            existingCustomer.TenantId = customer.TenantId;
            existingCustomer.Description = customer.Description;
            existingCustomer.IsActive = customer.IsActive;
            existingCustomer.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingCustomer;
        }

        public async Task<bool> DeleteCustomerAsync(string id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return false;

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
