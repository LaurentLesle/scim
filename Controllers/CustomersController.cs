using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScimServiceProvider.Models;
using ScimServiceProvider.Services;

namespace ScimServiceProvider.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Customer>>> GetCustomers()
        {
            try
            {
                var customers = await _customerService.GetAllCustomersAsync();
                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(string id)
        {
            try
            {
                var customer = await _customerService.GetCustomerAsync(id);
                if (customer == null)
                {
                    return NotFound(new { Error = "Customer not found" });
                }

                return Ok(customer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("byTenant/{tenantId}")]
        public async Task<ActionResult<Customer>> GetCustomerByTenantId(string tenantId)
        {
            try
            {
                var customer = await _customerService.GetCustomerByTenantIdAsync(tenantId);
                if (customer == null)
                {
                    return NotFound(new { Error = "Customer not found" });
                }

                return Ok(customer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Customer>> CreateCustomer([FromBody] Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdCustomer = await _customerService.CreateCustomerAsync(customer);
                return CreatedAtAction(nameof(GetCustomer), new { id = createdCustomer.Id }, createdCustomer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Customer>> UpdateCustomer(string id, [FromBody] Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var updatedCustomer = await _customerService.UpdateCustomerAsync(id, customer);
                if (updatedCustomer == null)
                {
                    return NotFound(new { Error = "Customer not found" });
                }

                return Ok(updatedCustomer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCustomer(string id)
        {
            try
            {
                var result = await _customerService.DeleteCustomerAsync(id);
                if (!result)
                {
                    return NotFound(new { Error = "Customer not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
