using Microsoft.EntityFrameworkCore;
using panelapp.Data;
using panelapp.Models;

namespace panelapp.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;

        public CustomerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void NormalizeCustomer(Customer model)
        {
            model.CustomerName = (model.CustomerName ?? string.Empty).Trim();

            model.VatNumber = string.IsNullOrWhiteSpace(model.VatNumber)
                ? string.Empty
                : model.VatNumber.Trim();

            model.Phone = string.IsNullOrWhiteSpace(model.Phone)
                ? null
                : new string(model.Phone.Where(char.IsDigit).Take(10).ToArray());

            model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            model.ContactPerson = string.IsNullOrWhiteSpace(model.ContactPerson) ? null : model.ContactPerson.Trim();
            model.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            model.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();
        }

        public async Task<CustomerValidationResult> ValidateCustomerAsync(Customer model, bool isEdit)
        {
            var result = new CustomerValidationResult();

            if (string.IsNullOrWhiteSpace(model.CustomerName))
            {
                result.Errors.Add(new CustomerValidationError
                {
                    FieldName = nameof(model.CustomerName),
                    ErrorMessage = "Το όνομα πελάτη είναι υποχρεωτικό."
                });
            }
            else if (await CustomerNameExistsAsync(
                         model.CustomerName,
                         isEdit ? model.CustomerID : null))
            {
                result.Errors.Add(new CustomerValidationError
                {
                    FieldName = nameof(model.CustomerName),
                    ErrorMessage = "Υπάρχει ήδη πελάτης με αυτό το όνομα."
                });
            }

            if (string.IsNullOrWhiteSpace(model.VatNumber))
            {
                result.Errors.Add(new CustomerValidationError
                {
                    FieldName = nameof(model.VatNumber),
                    ErrorMessage = "Το ΑΦΜ είναι υποχρεωτικό."
                });

                return result;
            }

            if (!model.VatNumber.All(char.IsDigit) || model.VatNumber.Length != 9)
            {
                result.Errors.Add(new CustomerValidationError
                {
                    FieldName = nameof(model.VatNumber),
                    ErrorMessage = "Το ΑΦΜ πρέπει να αποτελείται από 9 ψηφία."
                });

                return result;
            }

            if (await VatNumberExistsAsync(
                    model.VatNumber,
                    isEdit ? model.CustomerID : null))
            {
                result.Errors.Add(new CustomerValidationError
                {
                    FieldName = nameof(model.VatNumber),
                    ErrorMessage = "Υπάρχει ήδη πελάτης με αυτό το ΑΦΜ."
                });
            }

            return result;
        }

        private async Task<bool> CustomerNameExistsAsync(string customerName, int? excludeCustomerId = null)
        {
            return await _context.Customers.AnyAsync(c =>
                c.CustomerName == customerName &&
                (!excludeCustomerId.HasValue || c.CustomerID != excludeCustomerId.Value));
        }

        private async Task<bool> VatNumberExistsAsync(string vatNumber, int? excludeCustomerId = null)
        {
            return await _context.Customers.AnyAsync(c =>
                c.VatNumber == vatNumber &&
                (!excludeCustomerId.HasValue || c.CustomerID != excludeCustomerId.Value));
        }
    }
}