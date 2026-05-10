using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using panelapp.Data;
using panelapp.Extensions;
using panelapp.Helpers;
using panelapp.Models;
using panelapp.Security;
using panelapp.Services;
using panelapp.ViewModels;

namespace panelapp.Controllers
{
    [SessionAuthorize]
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogService _activityLogger;
        private const int DefaultPageSize = 15;
        private static readonly int[] AllowedPageSizes = { 5, 10, 15, 20 };

        public SuppliersController(ApplicationDbContext context, IActivityLogService activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }


        private static string? CleanPhone(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return new string(value.Where(char.IsDigit).Take(10).ToArray());
        }

        private static string? CleanEmail(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void NormalizeContactPerson(
            SupplierContactPersonInputViewModel contact,
            ModelStateDictionary modelState,
            string emptyNameMessage,
            string contactLabel)
        {
            contact.FullName = contact.FullName?.Trim() ?? string.Empty;
            contact.Phone = CleanPhone(contact.Phone);
            contact.Email = CleanEmail(contact.Email);

            if (string.IsNullOrWhiteSpace(contact.FullName))
            {
                modelState.AddModelError("", emptyNameMessage);
            }


            if (!string.IsNullOrWhiteSpace(contact.Email) && !contact.Email.Contains("@"))
            {
                modelState.AddModelError("", $"Το email του {contactLabel} '{contact.FullName}' δεν είναι έγκυρο.");
            }
        }


        private static void NormalizeContactPerson(
            SupplierContactPersonEditItem contact,
            ModelStateDictionary modelState,
            string emptyNameMessage,
            string contactLabel)
        {
            contact.FullName = contact.FullName?.Trim() ?? string.Empty;
            contact.Phone = CleanPhone(contact.Phone);
            contact.Email = CleanEmail(contact.Email);

            if (string.IsNullOrWhiteSpace(contact.FullName))
            {
                modelState.AddModelError("", emptyNameMessage);
            }


            if (!string.IsNullOrWhiteSpace(contact.Email) && !contact.Email.Contains("@"))
            {
                modelState.AddModelError("", $"Το email του {contactLabel} '{contact.FullName}' δεν είναι έγκυρο.");
            }
        }

        // =========================
        // INDEX
        // =========================
        public async Task<IActionResult> Index(string? searchTerm, int page = 1, int pageSize = DefaultPageSize, string statusFilter = "all")
        {
            if (!AllowedPageSizes.Contains(pageSize))
            {
                pageSize = DefaultPageSize;
            }

            var query = _context.Suppliers.AsNoTracking().Include(s => s.ContactPersons).AsQueryable();

            if (statusFilter == "active")
            {
                query = query.Where(s => s.Active);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim();

                query = query.Where(s =>
                    s.SupplierName.Contains(search) ||
                    (s.Email != null && s.Email.Contains(search)) ||
                    (s.Address != null && s.Address.Contains(search)) ||
                    s.ContactPersons.Any(c =>
                        c.FullName.Contains(search) ||
                        (c.Phone != null && c.Phone.Contains(search)) ||
                        (c.Email != null && c.Email.Contains(search))));
            }

            var totalCount = await query.CountAsync();
            var totalPages = PaginationHelper.GetTotalPages(totalCount, pageSize);
            page = PaginationHelper.NormalizePage(page, totalPages);

            var suppliers = await query
                .OrderByDescending(s => s.Active)
                .ThenBy(s => s.SupplierName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new SupplierIndexViewModel
            {
                Suppliers = suppliers,
                SearchTerm = searchTerm ?? string.Empty,
                StatusFilter = statusFilter,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };

            return View(model);

        }

        // =========================
        // CREATE (GET)
        // =========================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new SupplierCreateViewModel());
        }

        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupplierCreateViewModel model)
        {
            model.SupplierName = model.SupplierName?.Trim() ?? string.Empty;
            model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            model.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            model.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();



            foreach (var contact in model.ContactPersons)
            {
                NormalizeContactPerson(
                    contact,
                    ModelState,
                    "Υπάρχει υπεύθυνος επικοινωνίας χωρίς όνομα.",
                    "υπευθύνου");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var exists = await _context.Suppliers
                .AnyAsync(s => s.SupplierName == model.SupplierName);

            if (exists)
            {
                ModelState.AddModelError(nameof(model.SupplierName), "Υπάρχει ήδη προμηθευτής με αυτό το όνομα.");
                return View(model);
            }

            var supplier = new Supplier
            {
                SupplierName = model.SupplierName,
                Email = model.Email,
                Address = model.Address,
                Notes = model.Notes,
                Active = model.Active,
                CreatedDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            foreach (var contact in model.ContactPersons)
            {
                if (string.IsNullOrWhiteSpace(contact.FullName))
                    continue;

                supplier.ContactPersons.Add(new SupplierContactPerson
                {
                    FullName = contact.FullName,
                    Phone = contact.Phone,
                    Email = contact.Email,
                    Active = true
                });
            }

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            await _activityLogger.LogAsync(
                "Supplier",
                supplier.SupplierID,
                "Created",
                $"Δημιουργήθηκε προμηθευτής {supplier.SupplierName}",
                supplier.ContactPersons.Any()
                    ? $"Υπεύθυνοι επικοινωνίας: {supplier.ContactPersons.Count}"
                    : "Χωρίς υπεύθυνο επικοινωνίας");

            TempData["SuccessMessage"] = "Ο προμηθευτής δημιουργήθηκε επιτυχώς.";
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // EDIT (GET)
        // =========================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.ContactPersons)
                .FirstOrDefaultAsync(s => s.SupplierID == id);

            if (supplier == null)
            {
                return NotFound();
            }

            var model = new SupplierEditViewModel
            {
                SupplierID = supplier.SupplierID,
                SupplierName = supplier.SupplierName,
                Email = supplier.Email,
                Address = supplier.Address,
                Notes = supplier.Notes,
                Active = supplier.Active,
                ExistingContactPersons = supplier.ContactPersons
                    .OrderBy(c => c.FullName)
                    .Select(c => new SupplierContactPersonEditItem
                    {
                        SupplierContactPersonID = c.SupplierContactPersonID,
                        FullName = c.FullName,
                        Phone = c.Phone,
                        Email = c.Email,
                        Active = c.Active
                    })
                    .ToList()
            };

            return View(model);
        }

        // =========================
        // EDIT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SupplierEditViewModel model)
        {
            if (id != model.SupplierID)
            {
                return NotFound();
            }

            model.SupplierName = model.SupplierName?.Trim() ?? string.Empty;
            model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            model.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            model.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();

            foreach (var contact in model.ExistingContactPersons)
            {
                NormalizeContactPerson(
                    contact,
                    ModelState,
                    "Υπάρχει υπεύθυνος επικοινωνίας χωρίς όνομα.",
                    "υπευθύνου");
            }

            foreach (var contact in model.NewContactPersons)
            {
                NormalizeContactPerson(
                    contact,
                    ModelState,
                    "Υπάρχει νέος υπεύθυνος επικοινωνίας χωρίς όνομα.",
                    "νέου υπευθύνου");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var supplier = await _context.Suppliers
                .Include(s => s.ContactPersons)
                .FirstOrDefaultAsync(s => s.SupplierID == id);

            if (supplier == null)
            {
                return NotFound();
            }

            var duplicateNameExists = await _context.Suppliers
                .AnyAsync(s => s.SupplierID != id && s.SupplierName == model.SupplierName);

            if (duplicateNameExists)
            {
                ModelState.AddModelError(nameof(model.SupplierName), "Υπάρχει ήδη άλλος προμηθευτής με αυτό το όνομα.");
                return View(model);
            }

            supplier.SupplierName = model.SupplierName;
            supplier.Email = model.Email;
            supplier.Address = model.Address;
            supplier.Notes = model.Notes;
            supplier.Active = model.Active;
            supplier.LastModifiedDate = DateTime.Now;

            foreach (var contactModel in model.ExistingContactPersons)
            {
                var contact = supplier.ContactPersons
                    .FirstOrDefault(c => c.SupplierContactPersonID == contactModel.SupplierContactPersonID);

                if (contact == null)
                    continue;

                contact.FullName = contactModel.FullName;
                contact.Phone = contactModel.Phone;
                contact.Email = contactModel.Email;
                contact.Active = contactModel.Active;
            }

            foreach (var newContact in model.NewContactPersons)
            {
                if (string.IsNullOrWhiteSpace(newContact.FullName))
                    continue;

                supplier.ContactPersons.Add(new SupplierContactPerson
                {
                    FullName = newContact.FullName,
                    Phone = newContact.Phone,
                    Email = newContact.Email,
                    Active = true
                });
            }

            await _context.SaveChangesAsync();
            await _activityLogger.LogAsync(
                "Supplier",
                supplier.SupplierID,
                "Updated",
                $"Ενημερώθηκε προμηθευτής {supplier.SupplierName}",
                $"Κατάσταση: {(supplier.Active ? "Ενεργός" : "Ανενεργός")} · Υπεύθυνοι: {supplier.ContactPersons.Count}");

            TempData["SuccessMessage"] = "Ο προμηθευτής ενημερώθηκε επιτυχώς.";

            return RedirectToAction(nameof(Index));
        }


        //------------------------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!HttpContext.IsAdmin())
            {
                TempData["ErrorMessage"] = "Μόνο διαχειριστής μπορεί να διαγράψει προμηθευτή.";
                return RedirectToAction(nameof(Index));
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.SupplierID == id);

            if (supplier == null)
            {
                TempData["ErrorMessage"] = "Ο προμηθευτής δεν βρέθηκε.";
                return RedirectToAction(nameof(Index));
            }

            supplier.Active = false;
            supplier.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            await _activityLogger.LogAsync(
                "Supplier",
                supplier.SupplierID,
                "Deleted",
                $"Απενεργοποιήθηκε προμηθευτής {supplier.SupplierName}",
                "Ο προμηθευτής απενεργοποιηθηκε από την ενεργή λίστα.");

            TempData["SuccessMessage"] = "Ο προμηθευτής απενεργοποιηθηκε από την ενεργή λίστα.";

            return RedirectToAction(nameof(Index));
        }


        // =========================
        // DELETE CONTACT PERSON
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContactPerson(int id, int supplierId)
        {
            var supplier = await _context.Suppliers
                .Include(s => s.ContactPersons)
                .FirstOrDefaultAsync(s => s.SupplierID == supplierId);

            if (supplier == null)
            {
                TempData["ErrorMessage"] = "Ο προμηθευτής δεν βρέθηκε.";
                return RedirectToAction(nameof(Index));
            }

            var contact = supplier.ContactPersons
                .FirstOrDefault(c => c.SupplierContactPersonID == id);

            if (contact == null)
            {
                TempData["ErrorMessage"] = "Ο υπεύθυνος επικοινωνίας δεν βρέθηκε.";
                return RedirectToAction(nameof(Edit), new { id = supplierId });
            }

            _context.SupplierContactPersons.Remove(contact);
            supplier.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Ο υπεύθυνος επικοινωνίας διαγράφηκε επιτυχώς.";

            return RedirectToAction(nameof(Edit), new { id = supplierId });
        }



    }





}