using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using panelapp.Constants;
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
    public class PanelsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPanelCodeService _panelCodeService;
        private readonly IActivityLogService _activityLogger;
        private readonly IPanelExportService _panelExportService;
        private readonly IPanelMaterialService _panelMaterialService;
        private readonly IPanelService _panelService;

        private const int DefaultPageSize = 15;
        private static readonly int[] AllowedPageSizes = { 5, 10, 15, 20 };

        public PanelsController(ApplicationDbContext context, IPanelCodeService panelCodeService, IActivityLogService activityLogger, IPanelExportService panelExportService, IPanelMaterialService panelMaterialService, IPanelService panelService)
        {
            _context = context;
            _panelCodeService = panelCodeService;
            _activityLogger = activityLogger;
            _panelExportService = panelExportService;
            _panelMaterialService = panelMaterialService;
            _panelService = panelService;
        }



        private bool IsCompletedPanelLockedForCurrentUser(Panel panel)
        {
            return string.Equals(
                PanelStatuses.Normalize(panel.Status),
                PanelStatuses.Completed,
                StringComparison.OrdinalIgnoreCase)
                && !HttpContext.IsAdmin();

        }


        private async Task<List<SelectListItem>> GetCustomerOptionsAsync()
        {
            return await _context.Customers
                .AsNoTracking()
                .Where(c => c.Active)
                .OrderBy(c => c.CustomerName)
                .Select(c => new SelectListItem
                {
                    Value = c.CustomerID.ToString(),
                    Text = c.CustomerName
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetMaterialOptionsAsync()
        {
            return await _context.Materials
                .AsNoTracking()
                .Where(m => m.Active)
                .OrderBy(m => m.MaterialCode)
                .Select(m => new SelectListItem
                {
                    Value = m.MaterialID.ToString(),
                    Text = m.MaterialCode + " - " + m.Description
                })
                .ToListAsync();
        }

        private async Task<Panel?> GetPanelAsync(int id)
        {
            return await _context.Panels
                .FirstOrDefaultAsync(p => p.PanelID == id);
        }


        private async Task<List<PanelMaterialRowViewModel>> GetPanelMaterialRowsAsync(int panelId)
        {
            return await (
                from pm in _context.PanelMaterials.AsNoTracking()
                join m in _context.Materials.AsNoTracking() on pm.MaterialID equals m.MaterialID
                join s in _context.Suppliers.AsNoTracking() on pm.SupplierID equals s.SupplierID into supplierJoin
                from s in supplierJoin.DefaultIfEmpty()
                where pm.PanelID == panelId
                select new PanelMaterialRowViewModel
                {
                    PanelMaterialID = pm.PanelMaterialID,
                    MaterialCode = m.MaterialCode,
                    MaterialDescription = m.Description,
                    SupplierName = s != null ? s.SupplierName : "",
                    Quantity = pm.Quantity,
                    UnitPrice = pm.UnitPrice,
                    DiscountPercent = pm.DiscountPercent,
                    IsManualPrice = pm.IsManualPrice,
                    ManualPriceReason = pm.ManualPriceReason
                })
                .ToListAsync();
        }


        private async Task<List<SelectListItem>> GetSupplierOptionsAsync()
        {
            return await _context.Suppliers
                .AsNoTracking()
                .Where(s => s.Active)
                .OrderBy(s => s.SupplierName)
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierID.ToString(),
                    Text = s.SupplierName
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetFilteredMaterialOptionsAsync(int? supplierId, string? materialSearch)
        {
            var query = _context.Materials
                .AsNoTracking()
                .Where(m => m.Active)
                .AsQueryable();

            if (supplierId.HasValue)
            {
                query = query.Where(m => m.SupplierID == supplierId.Value);
            }

            if (!string.IsNullOrWhiteSpace(materialSearch))
            {
                var search = materialSearch.Trim();

                query = query.Where(m =>
                    m.MaterialCode.Contains(search) ||
                    m.Description.Contains(search));
            }

            return await query
                .OrderBy(m => m.MaterialCode)
                .Select(m => new SelectListItem
                {
                    Value = m.MaterialID.ToString(),
                    Text = m.MaterialCode + " - " + m.Description
                })
                .ToListAsync();
        }


        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, int page = 1, int pageSize = DefaultPageSize)
        {
            if (!AllowedPageSizes.Contains(pageSize))
            {
                pageSize = DefaultPageSize;
            }



            var query = _context.Panels
                .Include(p => p.Customer)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim();

                query = query.Where(p =>
                    p.PanelCode.Contains(search) ||
                    (p.Customer != null && p.Customer.CustomerName.Contains(search)) ||
                    p.CustomerName.Contains(search) ||
                    (p.Description != null && p.Description.Contains(search)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = PaginationHelper.GetTotalPages(totalCount, pageSize);
            page = PaginationHelper.NormalizePage(page, totalPages);

            var panels = await query
                .OrderByDescending(p => p.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new PanelIndexViewModel
            {
                Panels = panels,
                SearchTerm = searchTerm ?? string.Empty,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PanelFormViewModel
            {
                PanelCode = await _panelCodeService.GetNextPanelCodeAsync(),
                Status = PanelStatuses.UnderConstruction,
                Customers = await GetCustomerOptionsAsync()
            };

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PanelFormViewModel model)
        {
            model.Status = PanelStatuses.Normalize(model.Status);

            if (!PanelStatuses.IsAllowed(model.Status))
            {
                ModelState.AddModelError(nameof(model.Status), "Μη έγκυρη κατάσταση πίνακα.");
            }

            if (!ModelState.IsValid)
            {
                model.PanelCode = await _panelCodeService.GetNextPanelCodeAsync();
                model.Customers = await GetCustomerOptionsAsync();
                return View(model);
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == model.CustomerID && c.Active);

            if (customer == null)
            {
                ModelState.AddModelError(nameof(model.CustomerID), "Ο επιλεγμένος πελάτης δεν βρέθηκε.");
                model.PanelCode = await _panelCodeService.GetNextPanelCodeAsync();
                model.Customers = await GetCustomerOptionsAsync();
                return View(model);
            }

            const int maxAttempts = 3;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var panel = new Panel
                {
                    PanelCode = await _panelCodeService.GetNextPanelCodeAsync(),
                    CustomerID = customer.CustomerID,
                    CustomerName = customer.CustomerName,
                    Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                    Status = model.Status,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                };

                _context.Panels.Add(panel);

                try
                {
                    await _context.SaveChangesAsync();

                    await _activityLogger.LogAsync(
                        "Panel",
                        panel.PanelID,
                        "Created",
                        $"Δημιουργήθηκε ο πίνακας {panel.PanelCode}",
                        $"Πελάτης: {panel.CustomerName}");

                    TempData["SuccessMessage"] = $"Ο πίνακας {panel.PanelCode} δημιουργήθηκε επιτυχώς";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    _context.Panels.Remove(panel);

                    if (attempt == maxAttempts)
                    {
                        ModelState.AddModelError(string.Empty,
                            "Δεν ήταν δυνατή η δημιουργία μοναδικού κωδικού πίνακα. Προσπάθησε ξανά.");

                        model.PanelCode = await _panelCodeService.GetNextPanelCodeAsync();
                        model.Customers = await GetCustomerOptionsAsync();

                        return View(model);
                    }
                }
            }

            ModelState.AddModelError(string.Empty,
                "Δεν ήταν δυνατή η δημιουργία πίνακα. Προσπάθησε ξανά.");

            model.PanelCode = await _panelCodeService.GetNextPanelCodeAsync();
            model.Customers = await GetCustomerOptionsAsync();

            return View(model);
        }




        public async Task<IActionResult> Details(int id, int? supplierId, string? materialSearch)
        {
            var panel = await _context.Panels
                .AsNoTracking()
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.PanelID == id);

            if (panel == null)
            {
                return NotFound();
            }

            var vm = new PanelDetailsViewModel
            {
                Panel = panel,
                Materials = await GetPanelMaterialRowsAsync(id),
                AddMaterialForm = new AddMaterialToPanelViewModel
                {
                    PanelID = panel.PanelID,
                    PanelCode = panel.PanelCode,
                    SupplierID = supplierId,
                    MaterialSearch = materialSearch ?? string.Empty,
                    Quantity = 1,
                    DiscountPercent = 0,
                    Suppliers = await GetSupplierOptionsAsync(),
                    Materials = await GetFilteredMaterialOptionsAsync(supplierId, materialSearch)
                }
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> AddMaterial(int id)
        {
            var panel = await GetPanelAsync(id);

            if (panel == null)
            {
                return NotFound();
            }

            var materials = await GetMaterialOptionsAsync();

            var vm = new AddMaterialToPanelViewModel
            {
                PanelID = panel.PanelID,
                PanelCode = panel.PanelCode,
                Materials = materials
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMaterial(AddMaterialToPanelViewModel model)
        {
            var panel = await GetPanelAsync(model.PanelID);
            if (panel == null)
            {
                return NotFound();
            }

            if (IsCompletedPanelLockedForCurrentUser(panel))
            {
                TempData["ErrorMessage"] = "Ο πίνακας είναι ολοκληρωμένος. Μόνο διαχειριστής μπορεί να προσθέσει υλικά.";
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            if (!ModelState.IsValid)
            {
                model.Materials = await GetMaterialOptionsAsync();

                return View(model);
            }

            var material = await _context.Materials
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MaterialID == model.MaterialID);



            if (material == null)
            {
                return NotFound();
            }

            var panelMaterial = new panelapp.Models.PanelMaterial
            {
                PanelID = model.PanelID,
                MaterialID = model.MaterialID,
                SupplierID = material.SupplierID,
                Quantity = model.Quantity,
                UnitPrice = material.CurrentPrice,
                DiscountPercent = model.DiscountPercent,
                IsManualPrice = false,
                ManualPriceReason = null,
                DateAdded = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            _context.PanelMaterials.Add(panelMaterial);
            panel.LastModifiedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = model.PanelID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMaterialInline(AddMaterialToPanelViewModel model)
        {
            var panel = await GetPanelAsync(model.PanelID);

            if (panel == null)
            {
                return NotFound();
            }

            if (IsCompletedPanelLockedForCurrentUser(panel))
            {
                TempData["ErrorMessage"] = "Ο πίνακας είναι ολοκληρωμένος. Μόνο διαχειριστής μπορεί να προσθέσει υλικά.";
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            var result = await _panelMaterialService.AddMaterialInlineAsync(model);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            panel.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                "Panel",
                panel.PanelID,
                "Updated",
                $"Προστέθηκε υλικό στον πίνακα {panel.PanelCode}",
                $"{result.MaterialCode} · Ποσότητα: {model.Quantity}");

            TempData["SuccessMessage"] = $"Το υλικό {result.MaterialCode} προστέθηκε επιτυχώς.";

            return RedirectToAction(nameof(Details), new { id = model.PanelID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMaterial(int panelMaterialId, int panelId)
        {
            var panel = await GetPanelAsync(panelId);

            if (panel == null)
            {
                return NotFound();
            }

            if (IsCompletedPanelLockedForCurrentUser(panel))
            {
                TempData["ErrorMessage"] = "Ο πίνακας είναι ολοκληρωμένος. Μόνο διαχειριστής μπορεί να διαγράψει υλικά.";
                return RedirectToAction(nameof(Details), new { id = panelId });
            }

            var result = await _panelMaterialService.RemoveMaterialAsync(panelMaterialId);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Details), new { id = panelId });
            }

            panel.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                "Panel",
                panelId,
                "Updated",
                $"Αφαιρέθηκε υλικό από τον πίνακα {panel.PanelCode}",
                string.IsNullOrWhiteSpace(result.MaterialCode)
                    ? "Αφαίρεση υλικού"
                    : $"{result.MaterialCode} · Ποσότητα: {result.Quantity}");

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Details), new { id = panelId });
        }


        [HttpGet]
        public async Task<IActionResult> EditMaterial(int id)
        {
            var panelMaterial = await (
                from pm in _context.PanelMaterials
                join p in _context.Panels on pm.PanelID equals p.PanelID
                join m in _context.Materials on pm.MaterialID equals m.MaterialID
                where pm.PanelMaterialID == id
                select new EditPanelMaterialViewModel
                {
                    PanelMaterialID = pm.PanelMaterialID,
                    PanelID = pm.PanelID,
                    PanelCode = p.PanelCode,
                    MaterialCode = m.MaterialCode,
                    MaterialDescription = m.Description,
                    Quantity = pm.Quantity,
                    UnitPrice = pm.UnitPrice,
                    DiscountPercent = pm.DiscountPercent,
                    IsManualPrice = pm.IsManualPrice,
                    ManualPriceReason = pm.ManualPriceReason
                })
                .FirstOrDefaultAsync();

            if (panelMaterial == null)
            {
                return NotFound();
            }

            return View(panelMaterial);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMaterial(EditPanelMaterialViewModel model)
        {
            var panel = await GetPanelAsync(model.PanelID);

            if (panel == null)
            {
                return NotFound();
            }

            if (IsCompletedPanelLockedForCurrentUser(panel))
            {
                TempData["ErrorMessage"] = "Ο πίνακας είναι ολοκληρωμένος. Μόνο διαχειριστής μπορεί να επεξεργαστεί υλικά.";
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _panelMaterialService.EditMaterialAsync(model);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            panel.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                "Panel",
                model.PanelID,
                "Updated",
                $"Ενημερώθηκε υλικό στον πίνακα {model.PanelCode}",
                $"Νέα ποσότητα: {model.Quantity} · Έκπτωση: {model.DiscountPercent}%");

            TempData["SuccessMessage"] = $"O πίνακας {model.PanelCode} ενημερώθηκε επιτυχώς";

            return RedirectToAction(nameof(Details), new { id = model.PanelID });
        }

        [AdminOnly]
        [HttpGet]
        public async Task<IActionResult> EditMaterialAdmin(int id)
        {
            var item = await (
                from pm in _context.PanelMaterials
                join p in _context.Panels on pm.PanelID equals p.PanelID
                join m in _context.Materials on pm.MaterialID equals m.MaterialID
                where pm.PanelMaterialID == id
                select new EditPanelMaterialAdminViewModel
                {
                    PanelMaterialID = pm.PanelMaterialID,
                    PanelID = pm.PanelID,
                    PanelCode = p.PanelCode,
                    MaterialCode = m.MaterialCode,
                    MaterialDescription = m.Description,
                    Quantity = pm.Quantity,
                    DiscountPercent = pm.DiscountPercent,
                    UnitPrice = pm.UnitPrice,
                    IsManualPrice = pm.IsManualPrice,
                    ManualPriceReason = pm.ManualPriceReason
                })
                .FirstOrDefaultAsync();

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [AdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMaterialAdmin(EditPanelMaterialAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _panelMaterialService.EditMaterialAdminAsync(model);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            var panel = await GetPanelAsync(model.PanelID);

            if (panel != null)
            {
                panel.LastModifiedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                "Panel",
                model.PanelID,
                "Updated",
                $"Ενημερώθηκε υλικό στον πίνακα {model.PanelCode}",
                $"Τιμή: {model.UnitPrice:N2} € · Έκπτωση: {model.DiscountPercent}%");

            TempData["SuccessMessage"] = $"O πίνακας {model.PanelCode} ενημερώθηκε επιτυχώς";

            return RedirectToAction(nameof(Details), new { id = model.PanelID });
        }



        [HttpGet]

        public async Task<IActionResult> SearchMaterials(int? supplierId, string? term)
        {
            if (!supplierId.HasValue)
            {
                return Json(new
                {
                    items = new List<object>(),
                    totalCount = 0,
                    isLimited = false,
                    needsSearch = false,
                    message = "Επίλεξε προμηθευτή για να δεις υλικά."
                });
            }

            var query = _context.Materials
                .AsNoTracking()
                .Where(m => m.Active && m.SupplierID == supplierId.Value)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var search = term.Trim();
                query = query.Where(m =>
                    m.MaterialCode.Contains(search) ||
                    m.Description.Contains(search));
            }

            var totalCount = await query.CountAsync();

            // Αν δεν υπάρχει search term και τα υλικά είναι πολλά,
            // μην επιστρέφεις τεράστιο dropdown
            if (string.IsNullOrWhiteSpace(term) && totalCount > 100)
            {
                return Json(new
                {
                    items = new List<object>(),
                    totalCount,
                    isLimited = true,
                    needsSearch = true,
                    message = "Μεγάλος αριθμός υλικών. Πληκτρολόγησε αναζήτηση για να περιορίσεις τα αποτελέσματα."
                });
            }

            var materials = await query
                .OrderBy(m => m.MaterialCode)
                .Take(100)
                .Select(m => new
                {
                    value = m.MaterialID,
                    text = m.MaterialCode + " - " + m.Description
                })
                .ToListAsync();

            var isLimited = totalCount > materials.Count;

            return Json(new
            {
                items = materials,
                totalCount,
                isLimited,
                needsSearch = false,
                message = isLimited
                    ? $"Βρέθηκαν {totalCount} υλικά. Εμφανίζονται τα πρώτα 100."
                    : $"Βρέθηκαν {materials.Count} υλικά."
            });
        }




        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var panel = await GetPanelAsync(id);

            if (panel == null)
            {
                return NotFound();
            }


            if (IsCompletedPanelLockedForCurrentUser(panel))
            {
                TempData["ErrorMessage"] = "Δεν επιτρέπεται επεξεργασία ολοκληρωμένου πίνακα από απλό χρήστη.";
                return RedirectToAction(nameof(Details), new { id = panel.PanelID });
            }

            var model = new PanelFormViewModel
            {
                PanelID = panel.PanelID,
                PanelCode = panel.PanelCode,
                CustomerID = panel.CustomerID,
                CustomerName = panel.CustomerName,
                Description = panel.Description,
                Status = panel.Status,
                Customers = await GetCustomerOptionsAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PanelFormViewModel model)
        {
            if (!model.PanelID.HasValue)
            {
                return NotFound();
            }

            model.Status = PanelStatuses.Normalize(model.Status);

            if (!PanelStatuses.IsAllowed(model.Status))
            {
                ModelState.AddModelError(nameof(model.Status), "Μη έγκυρη κατάσταση πίνακα.");
            }

            var panel = await GetPanelAsync(model.PanelID.Value);

            if (panel == null)
            {
                return NotFound();
            }

            if (IsCompletedPanelLockedForCurrentUser(panel))
            {
                TempData["ErrorMessage"] = "Δεν επιτρέπεται επεξεργασία ολοκληρωμένου πίνακα από απλό χρήστη.";
                return RedirectToAction(nameof(Details), new { id = panel.PanelID });
            }

            if (!ModelState.IsValid)
            {
                model.Customers = await GetCustomerOptionsAsync();
                return View(model);
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerID == model.CustomerID && c.Active);

            if (customer == null)
            {
                ModelState.AddModelError(nameof(model.CustomerID), "Ο επιλεγμένος πελάτης δεν βρέθηκε.");
                model.Customers = await GetCustomerOptionsAsync();
                return View(model);
            }

            panel.CustomerID = customer.CustomerID;
            panel.CustomerName = customer.CustomerName;
            panel.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            panel.Status = model.Status;
            panel.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            await _activityLogger.LogAsync(
                "Panel",
                panel.PanelID,
                "Updated",
                $"Ενημερώθηκε ο πίνακας {panel.PanelCode}",
                $"Πελάτης: {panel.CustomerName}");

            TempData["SuccessMessage"] = $"O πίνακας {panel.PanelCode} ενημερώθηκε επιτυχώς";
            return RedirectToAction(nameof(Details), new { id = panel.PanelID });
        }



        [HttpGet]
        public async Task<IActionResult> Copy(int id)
        {
            var sourcePanel = await _context.Panels
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.PanelID == id);

            if (sourcePanel == null)
            {
                return NotFound();
            }

            var customers = await GetCustomerOptionsAsync();

            var model = new CopyPanelViewModel
            {
                SourcePanelID = sourcePanel.PanelID,
                SourcePanelCode = sourcePanel.PanelCode,
                SourceCustomerName = sourcePanel.CustomerName,
                SuggestedPanelCode = await _panelCodeService.GetNextPanelCodeAsync(),
                CustomerID = sourcePanel.CustomerID,
                Description = sourcePanel.Description,
                CopyMaterials = true,
                CopyDiscounts = true,
                CopyManualPrices = true,
                Customers = customers
            };

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Copy(CopyPanelViewModel model)
        {
            model.CopyMaterials = true;
            model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

            model.Customers = await GetCustomerOptionsAsync();

            var sourcePanel = await _context.Panels
                .FirstOrDefaultAsync(p => p.PanelID == model.SourcePanelID);

            if (sourcePanel == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerID == model.CustomerID && c.Active);

            if (customer == null)
            {
                ModelState.AddModelError(nameof(model.CustomerID), "Ο επιλεγμένος πελάτης δεν βρέθηκε.");
            }

            if (!ModelState.IsValid)
            {
                model.SourcePanelCode = sourcePanel.PanelCode;
                model.SourceCustomerName = sourcePanel.CustomerName;
                model.SuggestedPanelCode = await _panelCodeService.GetNextPanelCodeAsync();
                return View(model);
            }

            var result = await _panelService.CopyPanelAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);

                model.SourcePanelCode = sourcePanel.PanelCode;
                model.SourceCustomerName = sourcePanel.CustomerName;
                model.SuggestedPanelCode = await _panelCodeService.GetNextPanelCodeAsync();

                return View(model);
            }

            await _activityLogger.LogAsync(
                "Panel",
                result.NewPanelID,
                "Created",
                $"Δημιουργήθηκε αντίγραφο πίνακα {result.NewPanelCode}",
                $"Αντιγραφή από {sourcePanel.PanelCode} · Πελάτης: {customer!.CustomerName}");

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Details), new { id = result.NewPanelID });
        }

        [HttpGet]
        public async Task<IActionResult> ExportCSV(int id)
        {
            var panel = await _context.Panels.FindAsync(id);

            if (panel == null)
            {
                return NotFound();
            }

            var bytes = await _panelExportService.ExportCsvAsync(id);

            return File(bytes, "text/csv", $"{panel.PanelCode}_Export.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(int id)
        {
            var panel = await _context.Panels
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PanelID == id);

            if (panel == null)
            {
                return NotFound();
            }

            var bytes = await _panelExportService.ExportExcelAsync(id);

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{panel.PanelCode}_Export.xlsx");
        }

    }
}