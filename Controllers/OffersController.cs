using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using panelapp.Constants;
using panelapp.Data;
using panelapp.Models;
using panelapp.Services;
using panelapp.ViewModels;
namespace panelapp.Controllers
{
    public class OffersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOfferCodeService _offerCodeService;
        private readonly IOfferExportService _offerExportService;
        private readonly IOfferPdfService _offerPdfService;
        private readonly IActivityLogService _activityLogService;

        public OffersController(
            ApplicationDbContext context,
            IOfferCodeService offerCodeService,
            IOfferExportService offerExportService, IOfferPdfService offerPdfService, IActivityLogService activityLogService)
        {
            _context = context;
            _offerCodeService = offerCodeService;
            _offerExportService = offerExportService;
            _offerPdfService = offerPdfService;
            _activityLogService = activityLogService;
        }

        // =========================================
        // INDEX
        // =========================================

        public async Task<IActionResult> Index(
            string? search,
            string? status,
            int page = 1)
        {
            const int pageSize = 10;

            if (page < 1)
                page = 1;

            var query = _context.Offers
                .Include(x => x.Customer)
                .Include(x => x.Panel)
                .Include(x => x.OfferMaterials)
                .Include(x => x.OfferCabinets)
                .Include(x => x.OfferExtraItems)
                .Where(o => !o.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.OfferCode.Contains(search) ||
                    x.CustomerName.Contains(search) ||
                    (x.Description != null && x.Description.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            var totalItems = await query.CountAsync();

            var offers = await query
                .OrderByDescending(x => x.LastModifiedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Statuses = OfferStatuses.All;

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(offers);
        }

        // =========================================
        // CREATE GET
        // =========================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var offer = new Offer
            {
                OfferCode = await _offerCodeService.GenerateNextOfferCodeAsync(),
                Status = OfferStatuses.Draft
            };

            ViewBag.Customers = await _context.Customers
                .Where(x => x.Active)
                .OrderBy(x => x.CustomerName)
                .ToListAsync();

            return View(offer);
        }

        // =========================================
        // CREATE POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Offer offer)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Customers = await _context.Customers
                    .Where(x => x.Active)
                    .OrderBy(x => x.CustomerName)
                    .ToListAsync();

                return View(offer);
            }

            offer.CreatedDate = DateTime.UtcNow;
            offer.LastModifiedDate = DateTime.UtcNow;

            _context.Offers.Add(offer);

            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync(
                "Offer",
                offer.OfferID,
                "Created",
                "Δημιουργία προσφοράς",
                $"Δημιουργήθηκε η προσφορά {offer.OfferCode} για τον πελάτη {offer.CustomerName}.");

            return RedirectToAction(nameof(Details), new { id = offer.OfferID });
        }

        // =========================================
        // DETAILS
        // =========================================

        public async Task<IActionResult> Details(int id, int? supplierId, string? materialSearch)
        {
            var offer = await _context.Offers
                .Include(x => x.Customer)
                .Include(x => x.Panel)
                .Include(x => x.OfferCabinets)
                    .ThenInclude(x => x.Cabinet)
                .Include(x => x.OfferCabinets)
                    .ThenInclude(x => x.Supplier)
                .Include(x => x.OfferExtraItems)
                .Include(x => x.OfferMaterials)
                    .ThenInclude(x => x.Material)
                .Include(x => x.OfferMaterials)
                    .ThenInclude(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.OfferID == id);

            if (offer == null)
                return NotFound();

            var vm = new OfferDetailsViewModel
            {
                Offer = offer,

                AddMaterialForm = new AddMaterialToOfferViewModel
                {
                    OfferID = offer.OfferID,
                    OfferCode = offer.OfferCode,
                    SupplierID = supplierId,
                    MaterialSearch = materialSearch ?? string.Empty,
                    Quantity = 1,
                    DiscountPercent = 0,
                    Suppliers = await GetSupplierOptionsAsync()
                },

                PricingForm = new OfferPricingViewModel
                {
                    OfferID = offer.OfferID,
                    OfferCode = offer.OfferCode,
                    LaborCost = offer.LaborCost,
                    ProfitAmount = offer.ProfitAmount
                },
                AddCabinetForm = new AddCabinetToOfferViewModel
                {
                    OfferID = offer.OfferID,
                    Quantity = 1,
                    DiscountPercent = 0,
                    Suppliers = await GetSupplierOptionsAsync()
                },
                AddExtraItemForm = new AddOfferExtraItemViewModel
                {
                    OfferID = offer.OfferID,
                    Unit = "pcs",
                    Quantity = 1,
                    DiscountPercent = 0
                },
            };

            return View(vm);
        }


        // =========================================
        // ADD MATERIAL GET
        // =========================================

        [HttpGet]
        public async Task<IActionResult> AddMaterial(int id)
        {
            var offer = await _context.Offers
                .FirstOrDefaultAsync(x => x.OfferID == id);

            if (offer == null)
                return NotFound();

            ViewBag.OfferID = offer.OfferID;
            ViewBag.OfferCode = offer.OfferCode;

            ViewBag.Materials = await _context.Materials
                .Include(x => x.Supplier)
                .Where(x => x.Active)
                .OrderBy(x => x.MaterialCode)
                .ToListAsync();

            return View(new OfferMaterial
            {
                OfferID = offer.OfferID,
                Quantity = 1,
                DiscountPercent = 0
            });
        }


        // =========================================
        // ADD MATERIAL POST
        // =========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMaterial(OfferMaterial offerMaterial)
        {
            var offer = await _context.Offers
                .FirstOrDefaultAsync(x => x.OfferID == offerMaterial.OfferID);

            if (offer == null)
                return NotFound();

            var material = await _context.Materials
                .FirstOrDefaultAsync(x => x.MaterialID == offerMaterial.MaterialID);

            if (material == null)
            {
                ModelState.AddModelError("", "Το υλικό δεν βρέθηκε.");
            }

            if (offerMaterial.Quantity <= 0)
            {
                ModelState.AddModelError(nameof(offerMaterial.Quantity), "Η ποσότητα πρέπει να είναι μεγαλύτερη από το μηδέν.");
            }

            if (offerMaterial.DiscountPercent < 0 || offerMaterial.DiscountPercent > 100)
            {
                ModelState.AddModelError(nameof(offerMaterial.DiscountPercent), "Η έκπτωση πρέπει να είναι από 0 έως 100.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.OfferID = offer.OfferID;
                ViewBag.OfferCode = offer.OfferCode;

                ViewBag.Materials = await _context.Materials
                    .Include(x => x.Supplier)
                    .Where(x => x.Active)
                    .OrderBy(x => x.MaterialCode)
                    .ToListAsync();

                return View(offerMaterial);
            }

            offerMaterial.SupplierID = material!.SupplierID;
            offerMaterial.UnitPrice = offerMaterial.IsManualPrice
                ? offerMaterial.UnitPrice
                : material.CurrentPrice;

            offerMaterial.DateAdded = DateTime.UtcNow;
            offerMaterial.LastModifiedDate = DateTime.UtcNow;

            offer.LastModifiedDate = DateTime.UtcNow;

            _context.OfferMaterials.Add(offerMaterial);

            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync(
                "Offer",
                offerMaterial.OfferID,
                "Updated",
                "Προσθήκη υλικού προσφοράς",
                "Προστέθηκε υλικό στην προσφορά.");

            return RedirectToAction(nameof(Details), new { id = offerMaterial.OfferID });
        }


        private async Task<List<SelectListItem>> GetSupplierOptionsAsync()
        {
            return await _context.Suppliers
                .AsNoTracking()
                .Where(x => x.Active)
                .OrderBy(x => x.SupplierName)
                .Select(x => new SelectListItem
                {
                    Value = x.SupplierID.ToString(),
                    Text = x.SupplierName
                })
                .ToListAsync();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMaterialInline(AddMaterialToOfferViewModel model)
        {
            var offer = await _context.Offers
                .FirstOrDefaultAsync(x => x.OfferID == model.OfferID);

            if (offer == null)
                return NotFound();

            var material = await _context.Materials
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaterialID == model.MaterialID && x.Active);

            if (material == null)
            {
                TempData["ErrorMessage"] = "Το υλικό δεν βρέθηκε.";
                return RedirectToAction(nameof(Details), new { id = model.OfferID });
            }

            if (model.Quantity <= 0)
            {
                TempData["ErrorMessage"] = "Η ποσότητα πρέπει να είναι μεγαλύτερη από το μηδέν.";
                return RedirectToAction(nameof(Details), new { id = model.OfferID });
            }

            if (model.DiscountPercent < 0 || model.DiscountPercent > 100)
            {
                TempData["ErrorMessage"] = "Η έκπτωση πρέπει να είναι από 0 έως 100.";
                return RedirectToAction(nameof(Details), new { id = model.OfferID });
            }

            var offerMaterial = new OfferMaterial
            {
                OfferID = model.OfferID,
                MaterialID = material.MaterialID,
                SupplierID = material.SupplierID,
                Quantity = model.Quantity,
                UnitPrice = material.CurrentPrice,
                DiscountPercent = model.DiscountPercent,
                IsManualPrice = false,
                ManualPriceReason = null,
                DateAdded = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            _context.OfferMaterials.Add(offerMaterial);

            offer.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Το υλικό προστέθηκε επιτυχώς στην προσφορά.";

            return RedirectToAction(nameof(Details), new { id = model.OfferID });
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
                .Where(x => x.Active && x.SupplierID == supplierId.Value)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var search = term.Trim();

                query = query.Where(x =>
                    x.MaterialCode.Contains(search) ||
                    x.Description.Contains(search));
            }

            var totalCount = await query.CountAsync();

            if (string.IsNullOrWhiteSpace(term) && totalCount > 100)
            {
                return Json(new
                {
                    items = new List<object>(),
                    totalCount,
                    isLimited = true,
                    needsSearch = true,
                    message = "Μεγάλος αριθμός υλικών. Πληκτρολόγησε αναζήτηση."
                });
            }

            var materials = await query
                .OrderBy(x => x.MaterialCode)
                .Take(100)
                .Select(x => new
                {
                    value = x.MaterialID,
                    text = x.MaterialCode + " - " + x.Description + " | " + x.CurrentPrice.ToString("0.00") + " €"
                })
                .ToListAsync();

            return Json(new
            {
                items = materials,
                totalCount,
                isLimited = totalCount > materials.Count,
                needsSearch = false,
                message = $"Βρέθηκαν {materials.Count} υλικά."
            });
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePricing(OfferPricingViewModel model)
        {
            var offer = await _context.Offers
                .FirstOrDefaultAsync(x => x.OfferID == model.OfferID);

            string RowVersion = model.RowVersion;

            if (offer == null)
                return NotFound();

            if (model.LaborCost < 0 || model.ProfitAmount < 0)
            {
                TempData["ErrorMessage"] = "Τα εργατικά και το κέρδος δεν μπορούν να είναι αρνητικά.";
                return RedirectToAction(nameof(Details), new { id = model.OfferID });
            }

            offer.LaborCost = model.LaborCost;
            offer.ProfitAmount = model.ProfitAmount;
            offer.LastModifiedDate = DateTime.UtcNow;


            _context.Entry(offer)
            .Property(x => x.RowVersion)
            .OriginalValue = Convert.FromBase64String(RowVersion);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] =
                    "Η προσφορά ενημερώθηκε από άλλο χρήστη. Κάνε ανανέωση και προσπάθησε ξανά.";

                return RedirectToAction(nameof(Details), new { id = offer.OfferID });
            }

            TempData["SuccessMessage"] = "Η κοστολόγηση ενημερώθηκε επιτυχώς.";

            return RedirectToAction(nameof(Details), new { id = model.OfferID });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duplicate(int id)
        {
            var sourceOffer = await _context.Offers
                .Include(x => x.OfferMaterials)
                .FirstOrDefaultAsync(x => x.OfferID == id);

            if (sourceOffer == null)
                return NotFound();

            var newOffer = new Offer
            {
                OfferCode = await _offerCodeService.GenerateNextOfferCodeAsync(),

                CustomerID = sourceOffer.CustomerID,
                CustomerName = sourceOffer.CustomerName,

                Description = sourceOffer.Description,
                Notes = sourceOffer.Notes,

                Status = OfferStatuses.Draft,

                LaborCost = sourceOffer.LaborCost,
                ProfitAmount = sourceOffer.ProfitAmount,

                CreatedDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            foreach (var item in sourceOffer.OfferMaterials)
            {
                newOffer.OfferMaterials.Add(new OfferMaterial
                {
                    MaterialID = item.MaterialID,
                    SupplierID = item.SupplierID,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPercent = item.DiscountPercent,
                    IsManualPrice = item.IsManualPrice,
                    ManualPriceReason = item.ManualPriceReason,
                    DateAdded = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                });
            }

            _context.Offers.Add(newOffer);
            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                "Offer",
                newOffer.OfferID,
                "Created",
                "Αντιγραφή προσφοράς",
                $"Δημιουργήθηκε νέα προσφορά {newOffer.OfferCode} από την {sourceOffer.OfferCode}.");

            TempData["SuccessMessage"] = "Δημιουργήθηκε νέα προσφορά από την υπάρχουσα.";

            return RedirectToAction(nameof(Details), new { id = newOffer.OfferID });
        }


        public async Task<IActionResult> ExportExcel(int id)
        {
            var bytes = await _offerExportService.ExportExcelAsync(id);

            if (bytes.Length == 0)
                return NotFound();

            var fileName = $"Offer_{id}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            await _activityLogService.LogAsync(
                "Offer",
                id,
                "Exported",
                "Export προσφοράς σε Excel",
                $"Έγινε εξαγωγή της προσφοράς #{id} σε Excel.");

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }


        public async Task<IActionResult> ExportCustomerOfferPdf(int id)
        {
            var bytes = await _offerPdfService.GenerateCustomerOfferPdfAsync(id);

            if (bytes.Length == 0)
                return NotFound();

            var fileName = $"Offer_{id}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

            await _activityLogService.LogAsync(
                "Offer",
                id,
                "Exported",
                "Export προσφοράς σε PDF",
                $"Έγινε εξαγωγή της προσφοράς #{id} σε PDF.");

            return File(bytes, "application/pdf", fileName);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleMaterial(int id)
        {
            var offerMaterial = await _context.OfferMaterials
                .Include(x => x.Offer)
                .FirstOrDefaultAsync(x => x.OfferMaterialID == id);

            if (offerMaterial == null)
                return NotFound();

            var offerId = offerMaterial.OfferID;

            if (offerMaterial.Offer != null)
            {
                offerMaterial.Offer.LastModifiedDate = DateTime.Now;
            }

            _context.OfferMaterials.Remove(offerMaterial);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Το υλικό αφαιρέθηκε από την προσφορά.";

            return RedirectToAction(nameof(Details), new { id = offerId });
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int offerId, string status)
        {
            var offer = await _context.Offers
                .FirstOrDefaultAsync(x => x.OfferID == offerId);

            if (offer == null)
                return NotFound();

            if (!OfferStatuses.All.Contains(status))
            {
                TempData["ErrorMessage"] = "Μη έγκυρη κατάσταση προσφοράς.";
                return RedirectToAction(nameof(Details), new { id = offerId });
            }

            offer.Status = status;
            offer.LastModifiedDate = DateTime.Now;

            if (status == OfferStatuses.Sent && offer.SentDate == null)
                offer.SentDate = DateTime.Now;

            if (status == OfferStatuses.Accepted && offer.AcceptedDate == null)
                offer.AcceptedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                "Offer",
                offer.OfferID,
                "Updated",
                "Αλλαγή κατάστασης προσφοράς",
                $"Η προσφορά {offer.OfferCode} άλλαξε κατάσταση σε {offer.Status}.");

            TempData["SuccessMessage"] = "Η κατάσταση της προσφοράς ενημερώθηκε.";

            return RedirectToAction(nameof(Details), new { id = offerId });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertToPanel(int id)
        {
            var offer = await _context.Offers
                .Include(x => x.OfferMaterials)
                .FirstOrDefaultAsync(x => x.OfferID == id);

            if (offer == null)
                return NotFound();

            if (offer.PanelID.HasValue)
            {
                TempData["ErrorMessage"] = "Η προσφορά έχει ήδη μετατραπεί σε πίνακα.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var panel = new Panel
            {
                PanelCode = $"PNL-{DateTime.Now:yyyyMMddHHmmss}",
                CustomerID = offer.CustomerID,
                CustomerName = offer.CustomerName,
                Description = offer.Description,
                Status = "Under Construction",
                LaborCost = offer.LaborCost,
                ProfitAmount = offer.ProfitAmount,
                SourceOfferID = offer.OfferID,
                CreatedDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            foreach (var item in offer.OfferMaterials)
            {
                panel.PanelMaterials.Add(new PanelMaterial
                {
                    MaterialID = item.MaterialID,
                    SupplierID = item.SupplierID,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPercent = item.DiscountPercent,
                    IsManualPrice = item.IsManualPrice,
                    ManualPriceReason = item.ManualPriceReason,
                    DateAdded = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                });
            }

            _context.Panels.Add(panel);

            offer.Status = OfferStatuses.Converted;
            offer.ConvertedDate = DateTime.Now;
            offer.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            offer.PanelID = panel.PanelID;

            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync(
                "Offer",
                offer.OfferID,
                "Converted",
                "Μετατροπή προσφοράς σε πίνακα",
                $"Η προσφορά {offer.OfferCode} μετατράπηκε στον πίνακα {panel.PanelCode}.");
            await _activityLogService.LogAsync(
                "Panel",
                panel.PanelID,
                "Created",
                "Δημιουργία πίνακα από προσφορά",
                $"Ο πίνακας {panel.PanelCode} δημιουργήθηκε από την προσφορά {offer.OfferCode}.");
            TempData["SuccessMessage"] = "Η προσφορά μετατράπηκε σε πίνακα.";

            return RedirectToAction(nameof(Details), new { id = offer.OfferID });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMaterial(EditOfferMaterialViewModel model)
        {
            var offerMaterial = await _context.OfferMaterials
                .Include(x => x.Offer)
                .Include(x => x.Material)
                .FirstOrDefaultAsync(x => x.OfferMaterialID == model.OfferMaterialID);

            if (offerMaterial == null)
                return NotFound();

            if (model.Quantity <= 0)
            {
                TempData["ErrorMessage"] = "Η ποσότητα πρέπει να είναι μεγαλύτερη από το μηδέν.";
                return RedirectToAction(nameof(Details), new { id = offerMaterial.OfferID });
            }

            if (model.DiscountPercent < 0 || model.DiscountPercent > 100)
            {
                TempData["ErrorMessage"] = "Η έκπτωση πρέπει να είναι από 0 έως 100.";
                return RedirectToAction(nameof(Details), new { id = offerMaterial.OfferID });
            }

            if (model.UnitPrice < 0)
            {
                TempData["ErrorMessage"] = "Η τιμή δεν μπορεί να είναι αρνητική.";
                return RedirectToAction(nameof(Details), new { id = offerMaterial.OfferID });
            }

            offerMaterial.Quantity = model.Quantity;
            offerMaterial.DiscountPercent = model.DiscountPercent;
            offerMaterial.IsManualPrice = model.IsManualPrice;

            if (model.IsManualPrice)
            {
                offerMaterial.UnitPrice = model.UnitPrice;
                offerMaterial.ManualPriceReason = model.ManualPriceReason;
            }
            else
            {
                offerMaterial.UnitPrice = offerMaterial.Material?.CurrentPrice ?? offerMaterial.UnitPrice;
                offerMaterial.ManualPriceReason = null;
            }

            offerMaterial.LastModifiedDate = DateTime.UtcNow;

            if (offerMaterial.Offer != null)
            {
                offerMaterial.Offer.LastModifiedDate = DateTime.UtcNow;
            }

            _context.Entry(offerMaterial)
                .Property(x => x.RowVersion)
                .OriginalValue = Convert.FromBase64String(model.RowVersion);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] =
                    "Το υλικό ενημερώθηκε από άλλο χρήστη. Κάνε ανανέωση και προσπάθησε ξανά.";

                return RedirectToAction(nameof(Details), new { id = offerMaterial.OfferID });
            }

            TempData["SuccessMessage"] = "Το υλικό ενημερώθηκε.";

            return RedirectToAction(nameof(Details), new { id = offerMaterial.OfferID });
        }


        [HttpGet]
        public async Task<IActionResult> SearchCabinets(int? supplierId, string? term)
        {
            if (!supplierId.HasValue)
            {
                return Json(new
                {
                    items = new List<object>(),
                    totalCount = 0,
                    isLimited = false,
                    needsSearch = false,
                    message = "Επίλεξε προμηθευτή για να δεις ερμάρια."
                });
            }

            var query = _context.Cabinets
                .AsNoTracking()
                .Where(x => x.Active && x.SupplierID == supplierId.Value)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var search = term.Trim();

                query = query.Where(x =>
                    x.CabinetCode.Contains(search) ||
                    x.Description.Contains(search));
            }

            var totalCount = await query.CountAsync();

            if (string.IsNullOrWhiteSpace(term) && totalCount > 100)
            {
                return Json(new
                {
                    items = new List<object>(),
                    totalCount,
                    isLimited = true,
                    needsSearch = true,
                    message = "Μεγάλος αριθμός ερμαρίων. Πληκτρολόγησε αναζήτηση."
                });
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
                totalCount,
                isLimited = totalCount > cabinets.Count,
                needsSearch = false,
                message = $"Βρέθηκαν {cabinets.Count} ερμάρια."
            });
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCabinetInline(AddCabinetToOfferViewModel model)
        {
            var offer = await _context.Offers
                .FirstOrDefaultAsync(x => x.OfferID == model.OfferID);

            if (offer == null)
                return NotFound();

            var cabinet = await _context.Cabinets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CabinetID == model.CabinetID && x.Active);

            if (cabinet == null)
            {
                TempData["ErrorMessage"] = "Το ερμάριο δεν βρέθηκε.";
                return RedirectToAction(nameof(Details), new { id = model.OfferID });
            }

            if (model.Quantity <= 0)
            {
                TempData["ErrorMessage"] = "Η ποσότητα πρέπει να είναι μεγαλύτερη από το μηδέν.";
                return RedirectToAction(nameof(Details), new { id = model.OfferID });
            }

            if (model.DiscountPercent < 0 || model.DiscountPercent > 100)
            {
                TempData["ErrorMessage"] = "Η έκπτωση πρέπει να είναι από 0 έως 100.";
                return RedirectToAction(nameof(Details), new { id = model.OfferID });
            }

            var offerCabinet = new OfferCabinet
            {
                OfferID = model.OfferID,
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

            _context.OfferCabinets.Add(offerCabinet);

            offer.LastModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync(
                "Offer",
                model.OfferID,
                "Updated",
                "Προσθήκη ερμάριου προσφοράς",
                "Προστέθηκε ερμάριο στην προσφορά.");

            TempData["SuccessMessage"] = "Το ερμάριο προστέθηκε επιτυχώς στην προσφορά.";

            return RedirectToAction(nameof(Details), new { id = model.OfferID });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCabinet(int id)
        {
            var offerCabinet = await _context.OfferCabinets
                .Include(x => x.Offer)
                .FirstOrDefaultAsync(x => x.OfferCabinetID == id);

            if (offerCabinet == null)
                return NotFound();

            var offerId = offerCabinet.OfferID;

            if (offerCabinet.Offer != null)
                offerCabinet.Offer.LastModifiedDate = DateTime.UtcNow;

            _context.OfferCabinets.Remove(offerCabinet);

            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync(
                "Offer",
                offerId,
                "Deleted",
                "Αφαίρεση ερμάριο προσφοράς",
                "Αφαιρέθηκε γραμμή κόστους από την προσφορά.");

            TempData["SuccessMessage"] = "Το ερμάριο αφαιρέθηκε από την προσφορά.";

            return RedirectToAction(nameof(Details), new { id = offerId });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCabinet(EditOfferCabinetViewModel model)
        {
            var offerCabinet = await _context.OfferCabinets
                .Include(x => x.Offer)
                .Include(x => x.Cabinet)
                .FirstOrDefaultAsync(x => x.OfferCabinetID == model.OfferCabinetID);

            if (offerCabinet == null)
                return NotFound();

            if (model.Quantity <= 0 || model.UnitPrice < 0 ||
                model.DiscountPercent < 0 || model.DiscountPercent > 100)
            {
                TempData["ErrorMessage"] = "Έλεγξε ποσότητα, τιμή και έκπτωση.";
                return RedirectToAction(nameof(Details), new { id = offerCabinet.OfferID });
            }

            offerCabinet.Quantity = model.Quantity;
            offerCabinet.DiscountPercent = model.DiscountPercent;
            offerCabinet.IsManualPrice = model.IsManualPrice;

            if (model.IsManualPrice)
            {
                offerCabinet.UnitPrice = model.UnitPrice;
                offerCabinet.ManualPriceReason = model.ManualPriceReason;
            }
            else
            {
                offerCabinet.UnitPrice = offerCabinet.Cabinet?.CurrentPrice ?? offerCabinet.UnitPrice;
                offerCabinet.ManualPriceReason = null;
            }

            offerCabinet.LastModifiedDate = DateTime.UtcNow;

            if (offerCabinet.Offer != null)
                offerCabinet.Offer.LastModifiedDate = DateTime.UtcNow;

            _context.Entry(offerCabinet)
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

                return RedirectToAction(nameof(Details), new { id = offerCabinet.OfferID });
            }

            TempData["SuccessMessage"] = "Το ερμάριο ενημερώθηκε.";

            return RedirectToAction(nameof(Details), new { id = offerCabinet.OfferID });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExtraItem(AddOfferExtraItemViewModel model)
        {
            var offer = await _context.Offers
                .FirstOrDefaultAsync(x => x.OfferID == model.OfferID);

            if (offer == null)
                return NotFound();

            if (model.Quantity <= 0 || model.UnitPrice < 0 ||
                model.DiscountPercent < 0 || model.DiscountPercent > 100)
            {
                TempData["ErrorMessage"] = "Έλεγξε ποσότητα, τιμή και έκπτωση.";
                return RedirectToAction(nameof(Details), new { id = model.OfferID });
            }

            var extraItem = new OfferExtraItem
            {
                OfferID = model.OfferID,
                ItemCode = model.ItemCode,
                Description = model.Description,
                Unit = model.Unit,
                Quantity = model.Quantity,
                UnitPrice = model.UnitPrice,
                DiscountPercent = model.DiscountPercent,
                DateAdded = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            _context.OfferExtraItems.Add(extraItem);

            offer.LastModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync(
            "Offer",
            model.OfferID,
            "Updated",
            "Προσθήκη λοιπού υλικού προσφοράς",
            "Προστέθηκε λοιπο υλικό στην προσφορά.");


            TempData["SuccessMessage"] = "Το λοιπό υλικό προστέθηκε στην προσφορά.";

            return RedirectToAction(nameof(Details), new { id = model.OfferID });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExtraItem(int id)
        {
            var extraItem = await _context.OfferExtraItems
                .Include(x => x.Offer)
                .FirstOrDefaultAsync(x => x.OfferExtraItemID == id);

            if (extraItem == null)
                return NotFound();

            var offerId = extraItem.OfferID;

            if (extraItem.Offer != null)
                extraItem.Offer.LastModifiedDate = DateTime.UtcNow;

            _context.OfferExtraItems.Remove(extraItem);

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
            "Offer",
            offerId,
            "Deleted",
            "Αφαίρεση άλλου υλικού προσφοράς",
            "Αφαιρέθηκε άλλο υλικό από την προσφορά.");

            TempData["SuccessMessage"] = "Το λοιπό υλικό αφαιρέθηκε από την προσφορά.";

            return RedirectToAction(nameof(Details), new { id = offerId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateExtraItem(EditOfferExtraItemViewModel model)
        {
            var extraItem = await _context.OfferExtraItems
                .Include(x => x.Offer)
                .FirstOrDefaultAsync(x => x.OfferExtraItemID == model.OfferExtraItemID);

            if (extraItem == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.Description) ||
                model.Quantity <= 0 ||
                model.UnitPrice < 0 ||
                model.DiscountPercent < 0 ||
                model.DiscountPercent > 100)
            {
                TempData["ErrorMessage"] = "Έλεγξε περιγραφή, ποσότητα, τιμή και έκπτωση.";
                return RedirectToAction(nameof(Details), new { id = extraItem.OfferID });
            }

            if (model.Unit != "pcs" && model.Unit != "meters")
            {
                TempData["ErrorMessage"] = "Μη έγκυρη μονάδα μέτρησης.";
                return RedirectToAction(nameof(Details), new { id = extraItem.OfferID });
            }

            extraItem.ItemCode = model.ItemCode;
            extraItem.Description = model.Description;
            extraItem.Unit = model.Unit;
            extraItem.Quantity = model.Quantity;
            extraItem.UnitPrice = model.UnitPrice;
            extraItem.DiscountPercent = model.DiscountPercent;
            extraItem.LastModifiedDate = DateTime.UtcNow;

            if (extraItem.Offer != null)
                extraItem.Offer.LastModifiedDate = DateTime.UtcNow;

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

                return RedirectToAction(nameof(Details), new { id = extraItem.OfferID });
            }

            TempData["SuccessMessage"] = "Το λοιπό υλικό ενημερώθηκε.";

            return RedirectToAction(nameof(Details), new { id = extraItem.OfferID });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var offer = await _context.Offers
                .FirstOrDefaultAsync(o => o.OfferID == id);

            if (offer == null)
            {
                TempData["ErrorMessage"] = "Η προσφορά δεν βρέθηκε.";
                return RedirectToAction(nameof(Index));
            }

            offer.IsDeleted = true;
            offer.DeletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync(
                "Offer",
                offer.OfferID,
                "Deleted",
                "Αφαίρεση προσφοράς",
                "Αφαιρέθηκε προσφορά.");

            TempData["SuccessMessage"] =
                $"Η προσφορά {offer.OfferCode} διαγράφηκε επιτυχώς.";

            return RedirectToAction(nameof(Index));
        }
    }
}