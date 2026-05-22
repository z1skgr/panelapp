using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using panelapp.Data;
using panelapp.Helpers;
using panelapp.Models;
using panelapp.Security;
using panelapp.Services;
using panelapp.ViewModels;

namespace panelapp.Controllers
{
    [SessionAuthorize]
    public class CustomersController : Controller
    {
        private const int DefaultCustomerPageSize = 15;
        private const int DefaultPanelsPerCustomer = 15;
        private static readonly int[] AllowedCustomerPageSizes = { 5, 10, 15, 20 };
        private static readonly int[] AllowedPanelsPerCustomer = { 5, 10, 15, 20 };


        private readonly ApplicationDbContext _context;
        private readonly IActivityLogService _activityLogger;
        private readonly ICustomerService _customerService;
        public CustomersController(
           ApplicationDbContext context,
           IActivityLogService activityLogger,
           ICustomerService customerService)
        {
            _context = context;
            _activityLogger = activityLogger;
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm, int? expandedCustomerId,
            int page = 1,
            int customerPageSize = DefaultCustomerPageSize,
            int panelsPerCustomer = DefaultPanelsPerCustomer)
        {
            if (!AllowedCustomerPageSizes.Contains(customerPageSize))
            {
                customerPageSize = DefaultCustomerPageSize;
            }

            if (!AllowedPanelsPerCustomer.Contains(panelsPerCustomer))
            {
                panelsPerCustomer = DefaultPanelsPerCustomer;
            }


            var query = _context.Customers
                .AsNoTracking()
                .Include(c => c.Panels)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim();

                query = query.Where(c =>
                    c.CustomerName.Contains(search) ||
                    c.VatNumber.Contains(search));
            }

            var totalCount = await query.CountAsync();


            var totalPages = PaginationHelper.GetTotalPages(totalCount, customerPageSize);

            page = PaginationHelper.NormalizePage(page, totalPages);

            var customers = await query
                .OrderBy(c => c.CustomerName)
                .Skip((page - 1) * customerPageSize)
                .Take(customerPageSize)
                .ToListAsync();

            var model = new CustomerIndexViewModel
            {
                Customers = customers,
                SearchTerm = searchTerm ?? string.Empty,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                CustomerPageSize = customerPageSize,
                PanelsPerCustomer = panelsPerCustomer,
                ExpandedCustomerId = expandedCustomerId,
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Customer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer model)
        {
            _customerService.NormalizeCustomer(model);

            var validation = await _customerService.ValidateCustomerAsync(model, isEdit: false);

            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.FieldName, error.ErrorMessage);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.CreatedDate = DateTime.Now;
            model.LastModifiedDate = DateTime.Now;

            _context.Customers.Add(model);

            try
            {
                await _context.SaveChangesAsync();
                await _activityLogger.LogAsync(
                    "Customer",
                    model.CustomerID,
                    "Created",
                    $"Δημιουργήθηκε πελάτης {model.CustomerName}",
                    $"ΑΦΜ: {model.VatNumber}");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Δεν ήταν δυνατή η αποθήκευση του πελάτη. Έλεγξε αν υπάρχει ήδη ίδιο όνομα ή ΑΦΜ.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Ο πελάτης {model.CustomerName} δημιουργήθηκε επιτυχώς.";
            return RedirectToAction(nameof(Index));
        }

        [AdminOnly]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null)
            {
                return NotFound();
            }

            ViewBag.HasPanels = await _context.Panels
                .AsNoTracking()
                .AnyAsync(p => p.CustomerID == id);
            return View(customer);
        }

        [AdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer model)
        {
            _customerService.NormalizeCustomer(model);

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerID == model.CustomerID);

            if (customer == null)
            {
                return NotFound();
            }

            var hasPanels = await _context.Panels
                .AsNoTracking()
                .AnyAsync(p => p.CustomerID == model.CustomerID);
            ViewBag.HasPanels = hasPanels;

            if (hasPanels)
            {
                if (!string.Equals(customer.CustomerName, model.CustomerName, StringComparison.Ordinal))
                {
                    ModelState.AddModelError(nameof(model.CustomerName),
                        "Δεν επιτρέπεται αλλαγή ονόματος πελάτη επειδή υπάρχουν συνδεδεμένοι πίνακες.");
                }

                if (!string.Equals(customer.VatNumber, model.VatNumber, StringComparison.Ordinal))
                {
                    ModelState.AddModelError(nameof(model.VatNumber),
                        "Δεν επιτρέπεται αλλαγή ΑΦΜ επειδή υπάρχουν συνδεδεμένοι πίνακες.");
                }
            }

            var validation = await _customerService.ValidateCustomerAsync(model, isEdit: true);

            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.FieldName, error.ErrorMessage);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            customer.Phone = model.Phone;
            customer.Email = model.Email;
            customer.ContactPerson = model.ContactPerson;
            customer.Address = model.Address;
            customer.Notes = model.Notes;
            customer.Active = model.Active;
            customer.LastModifiedDate = DateTime.Now;

            if (!hasPanels)
            {
                customer.CustomerName = model.CustomerName;
                customer.VatNumber = model.VatNumber;
            }

            try
            {
                await _context.SaveChangesAsync();
                await _activityLogger.LogAsync(
                    "Customer",
                    customer.CustomerID,
                    "Updated",
                    $"Ενημερώθηκε πελάτης {customer.CustomerName}",
                    $"ΑΦΜ: {customer.VatNumber}");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Δεν ήταν δυνατή η ενημέρωση του πελάτη. Έλεγξε αν υπάρχει ήδη ίδιο όνομα ή ΑΦΜ.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Ο πελάτης {customer.CustomerName} ενημερώθηκε επιτυχώς.";
            return RedirectToAction(nameof(Index));
        }


    }
}