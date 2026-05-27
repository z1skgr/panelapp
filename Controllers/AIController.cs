using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using panelapp.Constants;
using panelapp.Data;
using panelapp.Models;
using panelapp.Services;
using panelapp.Services.AI;
using panelapp.Services.AI.Chat;
using panelapp.ViewModels.AI.Chat;
using panelapp.ViewModels.AiOffers;
using System.Text.Json;

namespace panelapp.Controllers
{
    public class AIController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOfferAiParser _offerAiParser;
        private readonly IActivityLogService _activityLogger;

        private readonly IOfferAiSummaryService _offerAiSummaryService;
        private readonly IAiChatRouterService _aiChatRouterService;

        public AIController(ApplicationDbContext context,
            IOfferAiParser offerAiParser,
            IActivityLogService activityLogger,
            IOfferAiSummaryService offerAiSummaryService,
            IAiChatRouterService aiChatRouterService)
        {
            _context = context;
            _offerAiParser = offerAiParser;
            _activityLogger = activityLogger;
            _offerAiSummaryService = offerAiSummaryService;
            _aiChatRouterService = aiChatRouterService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateOfferSummary(int offerId, CancellationToken cancellationToken)
        {
            var summary = await _offerAiSummaryService.GenerateSummaryAsync(offerId, cancellationToken);

            return Json(new
            {
                success = true,
                summary
            });
        }





        [HttpPost]
        public async Task<IActionResult> Chat(
    [FromBody] AiChatRequestViewModel model,
    CancellationToken cancellationToken)
        {
            var response = await _aiChatRouterService.HandleAsync(
                model,
                cancellationToken);

            return Json(response);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OfferPreview(
            OfferAiInputViewModel model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View("OfferCreate", model);
            }

            var draft = await _offerAiParser.ParseAsync(model.Prompt, cancellationToken);

            var preview = new OfferAiPreviewViewModel
            {
                OriginalPrompt = model.Prompt,
                Draft = draft,
                ExtraItems = draft.ExtraItems,

            };
            preview.SerializedDraft = JsonSerializer.Serialize(draft, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });


            await ResolveCustomerAsync(preview, cancellationToken);
            await ResolveMaterialsAsync(preview, cancellationToken);
            await ResolveCabinetsAsync(preview, cancellationToken);

            return View("OfferPreview", preview);
        }

        private async Task ResolveCustomerAsync(
            OfferAiPreviewViewModel preview,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(preview.Draft.CustomerName))
                return;

            var customerName = preview.Draft.CustomerName.Trim();

            var customer = await _context.Customers
                .AsNoTracking()
                .Where(x => x.Active)
                .FirstOrDefaultAsync(x => x.CustomerName.Contains(customerName), cancellationToken);

            if (customer == null)
                return;

            preview.ResolvedCustomerID = customer.CustomerID;
            preview.ResolvedCustomerName = customer.CustomerName;
        }

        private async Task ResolveMaterialsAsync(
           OfferAiPreviewViewModel preview,
           CancellationToken cancellationToken)
        {
            foreach (var line in preview.Draft.Materials)
            {
                var resolved = new OfferAiResolvedCatalogLineViewModel
                {
                    SupplierName = line.SupplierName,
                    CodeOrDescription = line.CodeOrDescription,
                    Quantity = line.Quantity,
                    DiscountPercent = 0
                };

                var term = line.CodeOrDescription.Trim();

                var material = await _context.Materials
                    .AsNoTracking()
                    .Include(x => x.Supplier)
                    .Where(x => x.Active)
                    .FirstOrDefaultAsync(x => x.MaterialCode == term, cancellationToken);

                if (material == null)
                {
                    var query = _context.Materials
                        .AsNoTracking()
                        .Include(x => x.Supplier)
                        .Where(x => x.Active);

                    if (!string.IsNullOrWhiteSpace(line.SupplierName))
                    {
                        var supplierName = line.SupplierName.Trim();

                        query = query.Where(x =>
                            x.Supplier != null &&
                            x.Supplier.SupplierName.Contains(supplierName));
                    }

                    material = await query.FirstOrDefaultAsync(x =>
                        x.Description.Contains(term),
                        cancellationToken);
                }

                if (material == null)
                {
                    resolved.Message = "Δεν βρέθηκε υλικό.";
                }
                else
                {
                    resolved.DiscountPercent = material.Supplier?.DefaultDiscountPercent ?? 0;
                    resolved.ResolvedItemID = material.MaterialID;
                    resolved.ResolvedSupplierID = material.SupplierID;
                    resolved.ResolvedCode = material.MaterialCode;
                    resolved.ResolvedDescription = material.Description;
                    resolved.UnitPrice = material.CurrentPrice;
                    resolved.SupplierName = material.Supplier?.SupplierName;
                    resolved.Message = "Βρέθηκε.";
                }

                preview.Materials.Add(resolved);
            }
        }

        private async Task ResolveCabinetsAsync(
            OfferAiPreviewViewModel preview,
            CancellationToken cancellationToken)
        {
            foreach (var line in preview.Draft.Cabinets)
            {
                var resolved = new OfferAiResolvedCatalogLineViewModel
                {
                    SupplierName = line.SupplierName,
                    CodeOrDescription = line.CodeOrDescription,
                    Quantity = line.Quantity,
                    DiscountPercent = 0
                };

                var term = line.CodeOrDescription.Trim();

                var cabinet = await _context.Cabinets
                    .AsNoTracking()
                    .Include(x => x.Supplier)
                    .Where(x => x.Active)
                    .FirstOrDefaultAsync(x => x.CabinetCode == term, cancellationToken);

                if (cabinet == null)
                {
                    var query = _context.Cabinets
                        .AsNoTracking()
                        .Include(x => x.Supplier)
                        .Where(x => x.Active);

                    if (!string.IsNullOrWhiteSpace(line.SupplierName))
                    {
                        var supplierName = line.SupplierName.Trim();

                        query = query.Where(x =>
                            x.Supplier != null &&
                            x.Supplier.SupplierName.Contains(supplierName));
                    }

                    cabinet = await query.FirstOrDefaultAsync(x =>
                        x.Description.Contains(term),
                        cancellationToken);
                }

                if (cabinet == null)
                {
                    resolved.Message = "Δεν βρέθηκε ερμάριο.";
                }
                else
                {
                    resolved.DiscountPercent = cabinet.Supplier?.DefaultDiscountPercent ?? 0;
                    resolved.ResolvedItemID = cabinet.CabinetID;
                    resolved.ResolvedSupplierID = cabinet.SupplierID;
                    resolved.ResolvedCode = cabinet.CabinetCode;
                    resolved.ResolvedDescription = cabinet.Description;
                    resolved.UnitPrice = cabinet.CurrentPrice;
                    resolved.SupplierName = cabinet.Supplier?.SupplierName;
                    resolved.Message = "Βρέθηκε.";
                }

                preview.Cabinets.Add(resolved);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OfferPreviewFromDraft(
            string serializedDraft,
            string originalPrompt,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(serializedDraft))
            {
                TempData["ErrorMessage"] = "Δεν βρέθηκε draft προσφοράς.";
                return RedirectToAction("Index", "Offers");
            }

            var draft = JsonSerializer.Deserialize<OfferAiDraftViewModel>(
                serializedDraft,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (draft == null)
            {
                TempData["ErrorMessage"] = "Δεν ήταν δυνατή η ανάγνωση του draft.";
                return RedirectToAction("Index", "Offers");
            }

            draft.Materials ??= new();
            draft.Cabinets ??= new();
            draft.ExtraItems ??= new();

            var preview = new OfferAiPreviewViewModel
            {
                OriginalPrompt = originalPrompt ?? string.Empty,
                Draft = draft,
                ExtraItems = draft.ExtraItems
            };

            preview.SerializedDraft = JsonSerializer.Serialize(draft, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await ResolveCustomerAsync(preview, cancellationToken);
            await ResolveMaterialsAsync(preview, cancellationToken);
            await ResolveCabinetsAsync(preview, cancellationToken);

            return View("OfferPreview", preview);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOfferFromPreview(
    string serializedDraft,
    CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(serializedDraft))
            {
                TempData["ErrorMessage"] = "Δεν βρέθηκε draft προσφοράς.";
                return RedirectToAction("Index", "Offers");
            }

            var draft = JsonSerializer.Deserialize<OfferAiDraftViewModel>(
                serializedDraft,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (draft == null || string.IsNullOrWhiteSpace(draft.CustomerName))
            {
                TempData["ErrorMessage"] = "Το draft δεν είναι έγκυρο.";
                return RedirectToAction("Index", "Offers");
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(x =>
                    x.Active &&
                    x.CustomerName.Contains(draft.CustomerName),
                    cancellationToken);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Δεν βρέθηκε πελάτης.";
                return RedirectToAction("Index", "Offers");
            }

            var unresolvedMaterials = new List<string>();

            foreach (var line in draft.Materials)
            {
                var term = line.CodeOrDescription.Trim();

                var exists = await _context.Materials
                    .AsNoTracking()
                    .Where(x => x.Active)
                    .AnyAsync(x =>
                        x.MaterialCode == term ||
                        x.Description.Contains(term),
                        cancellationToken);

                if (!exists)
                {
                    unresolvedMaterials.Add(term);
                }
            }

            var unresolvedCabinets = new List<string>();

            foreach (var line in draft.Cabinets)
            {
                var term = line.CodeOrDescription.Trim();

                var exists = await _context.Cabinets
                    .AsNoTracking()
                    .Where(x => x.Active)
                    .AnyAsync(x =>
                        x.CabinetCode == term ||
                        x.Description.Contains(term),
                        cancellationToken);

                if (!exists)
                {
                    unresolvedCabinets.Add(term);
                }
            }

            if (unresolvedMaterials.Any() || unresolvedCabinets.Any())
            {
                TempData["ErrorMessage"] =
                    "Δεν δημιουργήθηκε η προσφορά γιατί υπάρχουν γραμμές που δεν αντιστοιχίστηκαν.";

                return RedirectToAction("Index", "Offers");
            }


            var offer = new Offer
            {
                OfferCode = await GenerateOfferCodeAsync(cancellationToken),
                CustomerID = customer.CustomerID,
                CustomerName = customer.CustomerName,
                Description = draft.Description,
                Status = OfferStatuses.Draft,
                LaborCost = draft.LaborCost,
                ProfitAmount = draft.ProfitAmount,
                CreatedDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                IsDeleted = false
            };


            _context.Offers.Add(offer);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var line in draft.Materials)
            {
                var term = line.CodeOrDescription.Trim();

                var material = await _context.Materials
                    .Include(x => x.Supplier)
                    .Where(x => x.Active)
                    .FirstOrDefaultAsync(x =>
                        x.MaterialCode == term ||
                        x.Description.Contains(term),
                        cancellationToken);

                if (material == null)
                    continue;

                var defaultDiscount = material.Supplier?.DefaultDiscountPercent ?? 0;

                _context.OfferMaterials.Add(new OfferMaterial
                {
                    OfferID = offer.OfferID,
                    MaterialID = material.MaterialID,
                    SupplierID = material.SupplierID,
                    Quantity = line.Quantity,
                    UnitPrice = material.CurrentPrice,
                    DiscountPercent = line.DiscountPercent > 0
                        ? line.DiscountPercent
                        : defaultDiscount,
                    IsManualPrice = false,
                    DateAdded = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                });
            }

            foreach (var line in draft.Cabinets)
            {
                var term = line.CodeOrDescription.Trim();

                var cabinet = await _context.Cabinets
                    .Include(x => x.Supplier)
                    .Where(x => x.Active)
                    .FirstOrDefaultAsync(x =>
                        x.CabinetCode == term ||
                        x.Description.Contains(term),
                        cancellationToken);

                if (cabinet == null)
                    continue;

                var defaultDiscount = cabinet.Supplier?.DefaultDiscountPercent ?? 0;

                _context.OfferCabinets.Add(new OfferCabinet
                {
                    OfferID = offer.OfferID,
                    CabinetID = cabinet.CabinetID,
                    SupplierID = cabinet.SupplierID,
                    Quantity = line.Quantity,
                    UnitPrice = cabinet.CurrentPrice,
                    DiscountPercent = line.DiscountPercent > 0
                        ? line.DiscountPercent
                        : defaultDiscount,
                    IsManualPrice = false,
                    DateAdded = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                });
            }

            foreach (var item in draft.ExtraItems)
            {
                _context.OfferExtraItems.Add(new OfferExtraItem
                {
                    OfferID = offer.OfferID,
                    ItemCode = item.ItemCode,
                    Description = item.Description,
                    Unit = item.Unit,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPercent = 0,
                    DateAdded = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _activityLogger.LogAsync(
                "Offer",
                offer.OfferID,
                "AI_CREATED",
                $"Δημιουργήθηκε προσφορά {offer.OfferCode} μέσω AI",
                $"Πελάτης: {offer.CustomerName} · Υλικά: {draft.Materials.Count} · Ερμάρια: {draft.Cabinets.Count} · Λοιπά: {draft.ExtraItems.Count} · Εργατικά: {draft.LaborCost:N2} € · Κέρδος: {draft.ProfitAmount:N2} €");

            TempData["SuccessMessage"] =
                $"Η προσφορά {offer.OfferCode} δημιουργήθηκε επιτυχώς από AI draft για τον πελάτη {offer.CustomerName}.";
            return RedirectToAction("Details", "Offers", new { id = offer.OfferID });
        }



        private async Task<string> GenerateOfferCodeAsync(CancellationToken cancellationToken)
        {
            var year = DateTime.Now.Year;
            var prefix = $"OFF-{year}-";

            var count = await _context.Offers
                .CountAsync(x => x.OfferCode.StartsWith(prefix), cancellationToken);

            return $"{prefix}{count + 1:0000}";
        }






    }
}