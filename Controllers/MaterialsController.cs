using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using panelapp.Data;
using panelapp.Helpers;
using panelapp.Models;
using panelapp.Security;
using panelapp.Services;
using panelapp.ViewModels;
using ZL_panelapp.ViewModels;

namespace panelapp.Controllers
{
    [SessionAuthorize]
    public class MaterialsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MaterialsController> _logger;
        private readonly IActivityLogService _activityLogger;
        private readonly IMaterialService _materialService;
        private readonly IMaterialImportService _materialImportService;

        private const int DefaultSuppliersPerPage = 5;
        private const int DefaultMaterialsPerSupplier = 100;

        private static readonly int[] AllowedSupplierPageSizes = { 5, 10, 15, 20 };
        private static readonly int[] AllowedMaterialsPerSupplier = { 25, 50, 100, 200 };

        public MaterialsController(ApplicationDbContext context, ILogger<MaterialsController> logger, IActivityLogService activityLogger, IMaterialService materialService, IMaterialImportService materialImportService)
        {
            _context = context;
            _logger = logger;
            _activityLogger = activityLogger;
            _materialService = materialService;
            _materialImportService = materialImportService;
        }

        private IQueryable<Supplier> BuildSuppliersQuery(string supplierFilter, int? supplierId)
        {
            var query = _context.Suppliers
                .AsNoTracking()
                .AsQueryable();

            if (supplierFilter == "active")
            {
                query = query.Where(s => s.Active);
            }

            if (supplierId.HasValue)
            {
                query = query.Where(s => s.SupplierID == supplierId.Value);
            }

            return query;
        }


        private IQueryable<MaterialListRow> BuildMaterialsQuery(int supplierId, string supplierName, string? searchTerm)
        {
            var query =
                from m in _context.Materials.AsNoTracking()
                where m.SupplierID == supplierId
                select new MaterialListRow
                {
                    MaterialID = m.MaterialID,
                    MaterialCode = m.MaterialCode,
                    Description = m.Description,
                    Unit = m.Unit,
                    CurrentPrice = m.CurrentPrice,
                    Active = m.Active,
                    SupplierID = m.SupplierID,
                    SupplierName = supplierName
                };

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.Trim();

                query = query.Where(m =>
                    (m.MaterialCode != null && m.MaterialCode.Contains(search)) ||
                    (m.Description != null && m.Description.Contains(search)));
            }

            return query;
        }






        public async Task<IActionResult> Index(string? searchTerm,
            int? supplierId,
            int? expandedSupplierId,
            int supplierPage = 1,
            int suppliersPerPage = DefaultSuppliersPerPage,
            int materialsPerSupplier = DefaultMaterialsPerSupplier,
            string supplierFilter = "all")
        {


            if (!AllowedSupplierPageSizes.Contains(suppliersPerPage))
            {
                suppliersPerPage = DefaultSuppliersPerPage;
            }

            if (!AllowedMaterialsPerSupplier.Contains(materialsPerSupplier))
            {
                materialsPerSupplier = DefaultMaterialsPerSupplier;
            }


            var suppliersQuery = BuildSuppliersQuery(supplierFilter, supplierId);


            var totalSupplierCount = await suppliersQuery.CountAsync();
            var totalSupplierPages = PaginationHelper.GetTotalPages(totalSupplierCount, suppliersPerPage);
            supplierPage = PaginationHelper.NormalizePage(supplierPage, totalSupplierPages);

            var suppliers = await suppliersQuery
                .OrderByDescending(s => s.Active)
                .ThenBy(s => s.SupplierName)
                .Skip((supplierPage - 1) * suppliersPerPage)
                .Take(suppliersPerPage)
                .ToListAsync();

            var supplierOptionsQuery = BuildSuppliersQuery(supplierFilter, null);

            var allSupplierOptions = await supplierOptionsQuery
                .OrderByDescending(s => s.Active)
                .ThenBy(s => s.SupplierName)
                .ToListAsync();

            var groups = new List<MaterialSupplierGroupViewModel>();

            foreach (var supplier in suppliers)
            {
                var materialPageKey = $"materialPage_{supplier.SupplierID}";
                var currentMaterialPage = 1;

                if (Request.Query.ContainsKey(materialPageKey) &&
                    int.TryParse(Request.Query[materialPageKey], out var parsedPage) &&
                    parsedPage > 0)
                {
                    currentMaterialPage = parsedPage;
                }

                var materialsQuery = BuildMaterialsQuery(
                    supplier.SupplierID,
                    supplier.SupplierName,
                    searchTerm);


                var totalMaterialCount = await materialsQuery.CountAsync();
                var totalMaterialPages = PaginationHelper.GetTotalPages(totalMaterialCount, materialsPerSupplier);
                currentMaterialPage = PaginationHelper.NormalizePage(currentMaterialPage, totalMaterialPages);

                var materials = await materialsQuery
                    .OrderBy(m => m.MaterialCode)
                    .Skip((currentMaterialPage - 1) * materialsPerSupplier)
                    .Take(materialsPerSupplier)
                    .ToListAsync();

                groups.Add(new MaterialSupplierGroupViewModel
                {
                    SupplierID = supplier.SupplierID,
                    SupplierName = supplier.SupplierName,
                    SupplierActive = supplier.Active,
                    Materials = materials,
                    CurrentMaterialPage = currentMaterialPage,
                    TotalMaterialPages = totalMaterialPages,
                    TotalMaterialCount = totalMaterialCount
                });
            }

            var model = new MaterialIndexViewModel
            {
                SearchTerm = searchTerm ?? string.Empty,
                SupplierFilterId = supplierId,
                SupplierFilter = supplierFilter,

                SupplierPage = supplierPage,
                TotalSupplierPages = totalSupplierPages,
                TotalSupplierCount = totalSupplierCount,
                SuppliersPerPage = suppliersPerPage,
                MaterialsPerSupplier = materialsPerSupplier,
                SupplierOptions = allSupplierOptions,
                Groups = groups,
                ExpandedSupplierId = expandedSupplierId,
            };

            return View(model);
        }


        [AdminOnly]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Suppliers = await GetSupplierSelectListAsync();
            return View(new Material());
        }


        [AdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Material model)
        {
            _materialService.NormalizeMaterial(model);
            _materialService.ValidateMaterialUnit(model, ModelState);

            var duplicateExists = await _materialService.MaterialCodeExistsForSupplierAsync(
                model.SupplierID,
                model.MaterialCode);

            if (duplicateExists)
            {
                ModelState.AddModelError(nameof(model.MaterialCode),
                    $"Υπάρχει ήδη υλικό με τον κωδικό {model.MaterialCode} για τον επιλεγμένο προμηθευτή.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Suppliers = await GetSupplierSelectListAsync();
                return View(model);
            }

            model.CreatedDate = DateTime.Now;
            model.LastModifiedDate = DateTime.Now;
            model.PriceUpdatedDate = DateTime.Now;

            _context.Materials.Add(model);

            try
            {
                await _context.SaveChangesAsync();

                var supplierName = await _context.Suppliers
                        .Where(s => s.SupplierID == model.SupplierID)
                        .Select(s => s.SupplierName)
                        .FirstOrDefaultAsync();
                await _activityLogger.LogAsync(
                    "Material",
                    model.MaterialID,
                    "Created",
                    $"Δημιουργήθηκε υλικό {model.MaterialCode}",
                    string.IsNullOrWhiteSpace(supplierName)
                        ? $"Τιμή: {model.CurrentPrice:N2} €"
                        : $"Προμηθευτής: {supplierName} · Τιμή: {model.CurrentPrice:N2} €");
            }
            catch (DbUpdateException ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;

                ModelState.AddModelError(string.Empty,
                    $"Σφάλμα βάσης κατά την αποθήκευση: {errorMessage}");

                ViewBag.Suppliers = await GetSupplierSelectListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Το υλικό {model.MaterialCode} δημιουργήθηκε επιτυχώς.";
            return RedirectToAction(nameof(Index));
        }

        [AdminOnly]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.MaterialID == id);

            if (material == null)
            {
                return NotFound();
            }

            ViewBag.Suppliers = await GetSupplierSelectListAsync();
            return View(material);
        }


        [AdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Material model)
        {
            _materialService.NormalizeMaterial(model);
            _materialService.ValidateMaterialUnit(model, ModelState);

            var duplicateExists = await _materialService.MaterialCodeExistsForSupplierAsync(
                model.SupplierID,
                model.MaterialCode,
                model.MaterialID);

            if (duplicateExists)
            {
                ModelState.AddModelError(nameof(model.MaterialCode),
                    $"Υπάρχει ήδη υλικό με τον κωδικό {model.MaterialCode} για τον επιλεγμένο προμηθευτή.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Suppliers = await GetSupplierSelectListAsync();
                return View(model);
            }

            var existing = await _context.Materials.FirstOrDefaultAsync(m => m.MaterialID == model.MaterialID);

            if (existing == null)
            {
                return NotFound();
            }

            var priceChanged = existing.CurrentPrice != model.CurrentPrice;

            existing.MaterialCode = model.MaterialCode;
            existing.Description = model.Description;
            existing.Unit = model.Unit;
            existing.CurrentPrice = model.CurrentPrice;
            existing.SupplierID = model.SupplierID;
            existing.Active = model.Active;
            existing.LastModifiedDate = DateTime.Now;

            if (priceChanged)
            {
                existing.PriceUpdatedDate = DateTime.Now;
            }

            try
            {
                await _context.SaveChangesAsync();

                var supplierName = await _context.Suppliers
                    .Where(s => s.SupplierID == existing.SupplierID)
                    .Select(s => s.SupplierName)
                    .FirstOrDefaultAsync();

                await _activityLogger.LogAsync(
                    "Material",
                    existing.MaterialID,
                    "Updated",
                    $"Ενημερώθηκε υλικό {existing.MaterialCode}",
                    priceChanged
                        ? $"Προμηθευτής: {supplierName} · Νέα τιμή: {existing.CurrentPrice:N2} €"
                        : $"Προμηθευτής: {supplierName}");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(nameof(model.MaterialCode),
                    $"Υπάρχει ήδη υλικό με τον κωδικό {model.MaterialCode} για τον επιλεγμένο προμηθευτή.");
                ViewBag.Suppliers = await GetSupplierSelectListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Το υλικό {existing.MaterialCode} ενημερώθηκε επιτυχώς.";
            return RedirectToAction(nameof(Index));
        }



        [AdminOnly]
        [HttpGet]
        public async Task<IActionResult> Import()
        {
            var vm = new ImportMaterialsViewModel
            {
                Suppliers = await GetActiveSupplierOptionsAsync()
            };

            return View(vm);
        }

        [AdminOnly]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(ImportMaterialsViewModel model)
        {
            model.Suppliers = await GetActiveSupplierOptionsAsync();

            if (!model.Suppliers.Any())
            {
                ModelState.AddModelError(string.Empty, "Δεν υπάρχουν διαθέσιμοι προμηθευτές. Καταχώρισε πρώτα έναν προμηθευτή.");
                return View(model);
            }

            if (!ModelState.IsValid || model.ExcelFile == null)
            {
                return View(model);
            }

            _logger.LogInformation("Excel import started for supplier {SupplierID}", model.SupplierID);

            var supplierExists = await _context.Suppliers
                .AnyAsync(s => s.SupplierID == model.SupplierID && s.Active);

            if (!supplierExists)
            {
                ModelState.AddModelError(nameof(model.SupplierID), "Ο προμηθευτής δεν βρέθηκε.");
                return View(model);
            }

            var result = await _materialImportService.ImportAsync(model.SupplierID, model.ExcelFile);

            model.InsertedCount = result.InsertedCount;
            model.UpdatedCount = result.UpdatedCount;
            model.SkippedCount = result.SkippedCount;
            model.Messages = result.Messages;

            var supplierName = await _context.Suppliers
                .Where(s => s.SupplierID == model.SupplierID)
                .Select(s => s.SupplierName)
                .FirstOrDefaultAsync();

            await _activityLogger.LogAsync(
                "Import",
                null,
                "Imported",
                "Έγινε εισαγωγή υλικών από Excel",
                $"Προμηθευτής: {supplierName} · Νέα: {result.InsertedCount}, Ενημερώσεις: {result.UpdatedCount}, Παραλείψεις: {result.SkippedCount}");

            _logger.LogInformation(
                "Material import completed for supplier {SupplierId}. Inserted: {Inserted}, Updated: {Updated}, Skipped: {Skipped}",
                model.SupplierID,
                result.InsertedCount,
                result.UpdatedCount,
                result.SkippedCount);

            return View(model);
        }

        private async Task<List<SelectListItem>> GetActiveSupplierOptionsAsync()
        {
            return await _context.Suppliers
                .Where(s => s.Active)
                .OrderBy(s => s.SupplierName)
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierID.ToString(),
                    Text = s.SupplierName
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetSupplierSelectListAsync()
        {
            return await GetActiveSupplierOptionsAsync();
        }
    }

}