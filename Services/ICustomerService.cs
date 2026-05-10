using panelapp.Models;

namespace panelapp.Services
{
    public interface ICustomerService
    {
        void NormalizeCustomer(Customer model);

        Task<CustomerValidationResult> ValidateCustomerAsync(Customer model, bool isEdit);
    }
}