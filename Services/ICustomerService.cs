using ScimServiceProvider.Models;

namespace ScimServiceProvider.Services
{
    public interface ICustomerService
    {
        Task<Customer?> GetCustomerAsync(string id);
        Task<Customer?> GetCustomerByTenantIdAsync(string tenantId);
        Task<List<Customer>> GetAllCustomersAsync();
        Task<Customer> CreateCustomerAsync(Customer customer);
        Task<Customer?> UpdateCustomerAsync(string id, Customer customer);
        Task<bool> DeleteCustomerAsync(string id);
    }
}
