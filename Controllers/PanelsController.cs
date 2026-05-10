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
        private readonly IPanelOfferPdfService _panelOfferPdfService;


        private const int DefaultPageSize = 15;
        private static readonly int[] AllowedPageSizes = { 5, 10, 15, 20 };

        public PanelsController(ApplicationDbContext context, IPanelCodeService panelCodeService, IActivityLogService activityLogger, IPanelExportService panelExportService, IPanelMaterialService panelMaterialService, IPanelService panelService, IPanelOfferPdfService panelOfferPdfService, IPanelOfferPdfService panelPdfService)
        {
            _context = context;
            _panelCodeService = panelCodeService;
            _activityLogger = activityLogger;
            _panelExportService = panelExportService;
            _panelMaterialService = panelMaterialService;
            _panelService = panelService;
            _panelOfferPdfService = panelOfferPdfService;
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
                .Where(p => !p.IsDeleted)
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

            var materials = await GetPanelMaterialRowsAsync(id);

            var cabinetRows = await _context.PanelCabinets
                .AsNoTracking()
                .Include(x => x.Cabinet)
                .Include(x => x.Supplier)
                .Where(x => x.PanelID == id)
                .OrderBy(x => x.Cabinet!.CabinetCode)
                .Select(x => new PanelCabinetRowViewModel
                {
                    PanelCabinetID = x.PanelCabinetID,
                    CabinetCode = x.Cabinet != null ? x.Cabinet.CabinetCode : "",
                    CabinetDescription = x.Cabinet != null ? x.Cabinet.Description : "",
                    SupplierName = x.Supplier != null ? x.Supplier.SupplierName : "",
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    DiscountPercent = x.DiscountPercent,
                    IsManualPrice = x.IsManualPrice,
                    ManualPriceReason = x.ManualPriceReason,
                    RowVersion = x.RowVersion
                })
                .ToListAsync();

            var extraItemRows = await _context.PanelExtraItems
                .AsNoTracking()
                .Where(x => x.PanelID == id)
                .OrderBy(x => x.Description)
                .Select(x => new PanelExtraItemRowViewModel
                {
                    PanelExtraItemID = x.PanelExtraItemID,
                    ItemCode = x.ItemCode,
                    Description = x.Description,
                    Unit = x.Unit,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    DiscountPercent = x.DiscountPercent,
                    RowVersion = x.RowVersion
                })
                .ToListAsync();





            var materialsTotalWithoutDiscount = materials.Sum(m => m.OriginalTotalPrice);
            var materialsNetTotal = materials.Sum(m => m.NetValue);
            var cabinetsNetTotal = cabinetRows.Sum(x => x.TotalPrice);
            var extraItemsNetTotal = extraItemRows.Sum(x => x.TotalPrice);

            var finalOfferTotal =
                materialsNetTotal
                + cabinetsNetTotal
                + extraItemsNetTotal
                + panel.LaborCost
                + panel.ProfitAmount;

            var vm = new PanelDetailsViewModel
            {
                Panel = panel,
                Materials = materials,
                Cabinets = cabinetRows,
                ExtraItems = extraItemRows,

                OfferPricingForm = new PanelOfferPricingViewModel
                {
                    PanelID = panel.PanelID,
                    PanelCode = panel.PanelCode,
                    LaborCost = panel.LaborCost,
                    ProfitAmount = panel.ProfitAmount
                },

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
                },
                OfferSummary = new PanelOfferSummaryViewModel
                {
                    MaterialsTotalWithoutDiscount = materialsTotalWithoutDiscount,
                    MaterialsNetTotal = materialsNetTotal,
                    CabinetsNetTotal = cabinetsNetTotal,
                    ExtraItemsNetTotal = extraItemsNetTotal,
                    LaborCost = panel.LaborCost,
                    ProfitAmount = panel.ProfitAmount,
                    FinalOfferTotal = finalOfferTotal
                },
                AddCabinetForm = new AddCabinetToPanelViewModel
                {
                    PanelID = panel.PanelID,
                    Quantity = 1,
                    DiscountPercent = 0,
                    Suppliers = await GetSupplierOptionsAsync()
                },

                AddExtraItemForm = new AddPanelExtraItemViewModel
                {
                    PanelID = panel.PanelID,
                    Unit = "pcs",
                    Quantity = 1,
                    DiscountPercent = 0
                },
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

            var panelMaterial = new PanelMaterial
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
                    ManualPriceReason = pm.ManualPriceReason,
                    RowVersion = Convert.ToBase64String(pm.RowVersion)
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

            panel.LastModifiedDate = DateTime.UtcNow;



            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] =
                    "Το υλικό ενημερώθηκε από άλλο χρήστη. Κάνε ανανέωση και προσπάθησε ξανά.";

                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

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
                panel.LastModifiedDate = DateTime.UtcNow;
            }


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] =
                    "Το υλικό ενημερώθηκε από άλλο χρήστη. Κάνε ανανέωση και προσπάθησε ξανά.";

                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

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

            await _activityLogger.LogAsync(
                "Panel",
                id,
                "Exported",
                "Export πίνακα σε CSV",
                $"Έγινε εξαγωγή του πίνακα #{id} σε CSV.");

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

            await _activityLogger.LogAsync(
                "Panel",
                id,
                "Exported",
                "Export πίνακα σε Excel",
                $"Έγινε εξαγωγή του πίνακα #{id} σε Excel.");

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{panel.PanelCode}_Export.xlsx");
        }


        [HttpGet]
        public async Task<IActionResult> ExportCustomerOfferPdf(int id)
        {
            var panel = await _context.Panels
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PanelID == id);

            if (panel == null)
            {
                return NotFound();
            }

            var bytes = await _panelOfferPdfService.GenerateCustomerOfferPdfAsync(id);

            return File(
                bytes,
                "application/pdf",
                $"{panel.PanelCode}_Offer.pdf");
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOfferPricing(PanelOfferPricingViewModel model)
        {
            var panel = await GetPanelAsync(model.PanelID);

            if (panel == null)
            {
                return NotFound();
            }

            if (IsCompletedPanelLockedForCurrentUser(panel))
            {
                TempData["ErrorMessage"] = "Ο πίνακας είναι ολοκληρωμένος. Μόνο διαχειριστής μπορεί να αλλάξει την προσφορά.";
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Υπάρχει σφάλμα στα στοιχεία προσφοράς.";
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            panel.LaborCost = model.LaborCost;
            panel.ProfitAmount = model.ProfitAmount;

            panel.LastModifiedDate = DateTime.UtcNow;
            _context.Entry(panel)
                .Property(x => x.RowVersion)
                .OriginalValue = Convert.FromBase64String(model.RowVersion);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] =
                    "Ο πίνακας ενημερώθηκε από άλλο χρήστη. Κάνε ανανέωση και προσπάθησε ξανά.";

                return RedirectToAction(nameof(Details), new { id = panel.PanelID });
            }

            await _activityLogger.LogAsync(
                "Panel",
                panel.PanelID,
                "Updated",
                $"Ενημερώθηκε το κόστος του πίνακα {panel.PanelCode}",
                $"Εργατικά: {panel.LaborCost:N2} € · Κέρδος: {panel.ProfitAmount:N2} €");

            TempData["SuccessMessage"] = "Τα στοιχεία προσφοράς ενημερώθηκαν επιτυχώς.";

            return RedirectToAction(nameof(Details), new { id = model.PanelID });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearOfferPricing(int panelId)
        {
            var panel = await GetPanelAsync(panelId);

            if (panel == null)
            {
                return NotFound();
            }

            if (IsCompletedPanelLockedForCurrentUser(panel))
            {
                TempData["ErrorMessage"] = "Ο πίνακας είναι ολοκληρωμένος. Μόνο διαχειριστής μπορεί να αλλάξει την προσφορά.";
                return RedirectToAction(nameof(Details), new { id = panelId });
            }

            panel.LaborCost = 0;
            panel.ProfitAmount = 0;
            panel.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                "Panel",
                panel.PanelID,
                "Updated",
                $"Αφαιρέθηκαν εργατικά και κέρδος από τον πίνακα {panel.PanelCode}",
                "Μηδενίστηκαν τα πρόσθετα ποσά προσφοράς.");

            TempData["SuccessMessage"] = "Τα εργατικά και το κέρδος αφαιρέθηκαν από την προσφορά.";

            return RedirectToAction(nameof(Details), new { id = panelId });
        }

        public async Task<IActionResult> ExportInternalOfferPdf(int id)
        {
            var bytes = await _panelOfferPdfService.GenerateInternalCostingPdfAsync(id);

            if (bytes.Length == 0)
                return NotFound();

            var fileName = $"Panel_Internal_Costing_{id}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

            return File(bytes, "application/pdf", fileName);
        }


        [HttpGet]
        public async Task<IActionResult> SearchCabinets(int? supplierId, string? term)
        {
            if (!supplierId.HasValue)
            {
                return Json(new
                {
                    items = new List<object>(),
                    message = "Επίλεξε προμηθευτή για να δεις ερμάρια."
                });
            }

            var query = _context.Cabinets
                .AsNoTracking()
                .Where(x => x.Active && x.SupplierID == supplierId.Value);

            if (!string.IsNullOrWhiteSpace(term))
            {
                var search = term.Trim();

                query = query.Where(x =>
                    x.CabinetCode.Contains(search) ||
                    x.Description.Contains(search));
            }

            var cabinets = await query
                .OrderBy(x => x.CabinetCode)
                .Take(100)
                .Select(x => new
                {
                    value = x.CabinetID,
                    text = x.CabinetCode + " - " + x.Description + " | " + x.CurrentPrice.ToString("0.00") + " €"
                })
                .ToListAsync();

            return Json(new
            {
                items = cabinets,
                message = cabinets.Any()
                    ? $"Βρέθηκαν {cabinets.Count} ερμάρια."
                    : "Δεν βρέθηκαν ερμάρια."
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCabinetInline(AddCabinetToPanelViewModel model)
        {
            var panel = await _context.Panels
                .FirstOrDefaultAsync(x => x.PanelID == model.PanelID);

            if (panel == null)
                return NotFound();

            var cabinet = await _context.Cabinets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CabinetID == model.CabinetID && x.Active);

            if (cabinet == null)
            {
                TempData["ErrorMessage"] = "Το ερμάριο δεν βρέθηκε.";
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            if (model.Quantity <= 0)
            {
                TempData["ErrorMessage"] = "Η ποσότητα πρέπει να είναι μεγαλύτερη από το μηδέν.";
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            if (model.DiscountPercent < 0 || model.DiscountPercent > 100)
            {
                TempData["ErrorMessage"] = "Η έκπτωση πρέπει να είναι από 0 έως 100.";
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            var panelCabinet = new PanelCabinet
            {
                PanelID = model.PanelID,
                CabinetID = cabinet.CabinetID,
                SupplierID = cabinet.SupplierID,
                Quantity = model.Quantity,
                UnitPrice = cabinet.CurrentPrice,
                DiscountPercent = model.DiscountPercent,
                IsManualPrice = false,
                ManualPriceReason = null,
                DateAdded = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            _context.PanelCabinets.Add(panelCabinet);

            panel.LastModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                "Panel",
                panel.PanelID,
                "Updated",
                $"Προστέθηκε υλικό στον πίνακα {panel.PanelCode}",
                $"{model.CabinetID} · Ποσότητα: {model.Quantity}");

            TempData["SuccessMessage"] = "Το ερμάριο προστέθηκε στον πίνακα.";

            return RedirectToAction(nameof(Details), new { id = model.PanelID });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCabinet(int id)
        {
            var panelCabinet = await _context.PanelCabinets
                .Include(x => x.Panel)
                .FirstOrDefaultAsync(x => x.PanelCabinetID == id);

            if (panelCabinet == null)
                return NotFound();

            var panelId = panelCabinet.PanelID;

            if (panelCabinet.Panel != null)
                panelCabinet.Panel.LastModifiedDate = DateTime.UtcNow;

            _context.PanelCabinets.Remove(panelCabinet);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Το ερμάριο αφαιρέθηκε από τον πίνακα.";

            return RedirectToAction(nameof(Details), new { id = panelId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCabinet(EditPanelCabinetViewModel model)
        {
            var panelCabinet = await _context.PanelCabinets
                .Include(x => x.Panel)
                .Include(x => x.Cabinet)
                .FirstOrDefaultAsync(x => x.PanelCabinetID == model.PanelCabinetID);

            if (panelCabinet == null)
                return NotFound();

            if (model.Quantity <= 0 ||
                model.UnitPrice < 0 ||
                model.DiscountPercent < 0 ||
                model.DiscountPercent > 100)
            {
                TempData["ErrorMessage"] = "Έλεγξε ποσότητα, τιμή και έκπτωση.";
                return RedirectToAction(nameof(Details), new { id = panelCabinet.PanelID });
            }

            panelCabinet.Quantity = model.Quantity;
            panelCabinet.DiscountPercent = model.DiscountPercent;
            panelCabinet.IsManualPrice = model.IsManualPrice;

            if (model.IsManualPrice)
            {
                panelCabinet.UnitPrice = model.UnitPrice;
                panelCabinet.ManualPriceReason = model.ManualPriceReason;
            }
            else
            {
                panelCabinet.UnitPrice = panelCabinet.Cabinet?.CurrentPrice ?? panelCabinet.UnitPrice;
                panelCabinet.ManualPriceReason = null;
            }

            panelCabinet.LastModifiedDate = DateTime.UtcNow;

            if (panelCabinet.Panel != null)
                panelCabinet.Panel.LastModifiedDate = DateTime.UtcNow;

            _context.Entry(panelCabinet)
                .Property(x => x.RowVersion)
                .OriginalValue = Convert.FromBase64String(model.RowVersion);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] =
                    "Το ερμάριο ενημερώθηκε από άλλο χρήστη. Κάνε ανανέωση και προσπάθησε ξανά.";

                return RedirectToAction(nameof(Details), new { id = panelCabinet.PanelID });
            }

            TempData["SuccessMessage"] = "Το ερμάριο ενημερώθηκε.";

            return RedirectToAction(nameof(Details), new { id = panelCabinet.PanelID });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExtraItem(AddPanelExtraItemViewModel model)
        {
            var panel = await _context.Panels
                .FirstOrDefaultAsync(x => x.PanelID == model.PanelID);

            if (panel == null)
                return NotFound();

            if (IsCompletedPanelLockedForCurrentUser(panel))
            {
                TempData["ErrorMessage"] = "Ο πίνακας είναι ολοκληρωμένος. Μόνο διαχειριστής μπορεί να προσθέσει λοιπά υλικά.";
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            if (string.IsNullOrWhiteSpace(model.Description) ||
                model.Quantity <= 0 ||
                model.UnitPrice < 0 ||
                model.DiscountPercent < 0 ||
                model.DiscountPercent > 100)
            {
                TempData["ErrorMessage"] = "Έλεγξε περιγραφή, ποσότητα, τιμή και έκπτωση.";
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            if (model.Unit != "pcs" && model.Unit != "meters")
            {
                TempData["ErrorMessage"] = "Μη έγκυρη μονάδα μέτρησης.";
                return RedirectToAction(nameof(Details), new { id = model.PanelID });
            }

            var extraItem = new PanelExtraItem
            {
                PanelID = model.PanelID,
                ItemCode = model.ItemCode,
                Description = model.Description,
                Unit = model.Unit,
                Quantity = model.Quantity,
                UnitPrice = model.UnitPrice,
                DiscountPercent = model.DiscountPercent,
                DateAdded = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            _context.PanelExtraItems.Add(extraItem);

            panel.LastModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                "Panel",
                panel.PanelID,
                "Updated",
                $"Προστέθηκε λοιπό υλικό στον πίνακα {panel.PanelCode}",
                $"{extraItem.Description} · Ποσότητα: {extraItem.Quantity:N2} · Τιμή: {extraItem.UnitPrice:N2} €");

            TempData["SuccessMessage"] = "Το λοιπό υλικό προστέθηκε στον πίνακα.";

            return RedirectToAction(nameof(Details), new { id = model.PanelID });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExtraItem(int id)
        {
            var extraItem = await _context.PanelExtraItems
                .Include(x => x.Panel)
                .FirstOrDefaultAsync(x => x.PanelExtraItemID == id);

            if (extraItem == null)
                return NotFound();

            var panelId = extraItem.PanelID;

            if (extraItem.Panel != null)
                extraItem.Panel.LastModifiedDate = DateTime.UtcNow;

            _context.PanelExtraItems.Remove(extraItem);

            await _context.SaveChangesAsync();
            await _activityLogger.LogAsync(
                "Panel",
                panelId,
                "Deleted",
                "Αφαίρεση λοιπού υλικού",
                "Αφαιρέθηκε λοιπό υλικό από τον πίνακα.");


            TempData["SuccessMessage"] = "Το λοιπό υλικό αφαιρέθηκε από τον πίνακα.";

            return RedirectToAction(nameof(Details), new { id = panelId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateExtraItem(EditPanelExtraItemViewModel model)
        {
            var extraItem = await _context.PanelExtraItems
                .Include(x => x.Panel)
                .FirstOrDefaultAsync(x => x.PanelExtraItemID == model.PanelExtraItemID);

            if (extraItem == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.Description) ||
                model.Quantity <= 0 ||
                model.UnitPrice < 0 ||
                model.DiscountPercent < 0 ||
                model.DiscountPercent > 100)
            {
                TempData["ErrorMessage"] = "Έλεγξε περιγραφή, ποσότητα, τιμή και έκπτωση.";
                return RedirectToAction(nameof(Details), new { id = extraItem.PanelID });
            }

            if (model.Unit != "pcs" && model.Unit != "meters")
            {
                TempData["ErrorMessage"] = "Μη έγκυρη μονάδα μέτρησης.";
                return RedirectToAction(nameof(Details), new { id = extraItem.PanelID });
            }

            extraItem.ItemCode = model.ItemCode;
            extraItem.Description = model.Description;
            extraItem.Unit = model.Unit;
            extraItem.Quantity = model.Quantity;
            extraItem.UnitPrice = model.UnitPrice;
            extraItem.DiscountPercent = model.DiscountPercent;
            extraItem.LastModifiedDate = DateTime.UtcNow;

            if (extraItem.Panel != null)
                extraItem.Panel.LastModifiedDate = DateTime.UtcNow;

            _context.Entry(extraItem)
                .Property(x => x.RowVersion)
                .OriginalValue = Convert.FromBase64String(model.RowVersion);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] =
                    "Το λοιπό υλικό ενημερώθηκε από άλλο χρήστη. Κάνε ανανέωση και προσπάθησε ξανά.";

                return RedirectToAction(nameof(Details), new { id = extraItem.PanelID });
            }



            TempData["SuccessMessage"] = "Το λοιπό υλικό ενημερώθηκε.";

            return RedirectToAction(nameof(Details), new { id = extraItem.PanelID });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var panel = await _context.Panels.FindAsync(id);

            if (panel == null)
                return NotFound();

            panel.IsDeleted = true;
            panel.DeletedDate = DateTime.UtcNow;

            await _activityLogger.LogAsync(
                "Panel",
                panel.PanelID,
                "Deleted",
                $"Διαγράφηκε πίνακας",
                $"Διαγράφηκε ο πίνακας {panel.PanelCode}"
            );

            TempData["SuccessMessage"] =
                $"Ο πίνακας {panel.PanelCode} διαγράφηκε επιτυχώς.";
            return RedirectToAction(nameof(Index));
        }

    }


}