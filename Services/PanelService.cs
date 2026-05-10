using Microsoft.EntityFrameworkCore;
using panelapp.Constants;
using panelapp.Data;
using panelapp.Models;
using panelapp.Services.Results;
using panelapp.ViewModels;

namespace panelapp.Services
{
    public class PanelService : IPanelService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPanelCodeService _panelCodeService;

        public PanelService(
            ApplicationDbContext context,
            IPanelCodeService panelCodeService)
        {
            _context = context;
            _panelCodeService = panelCodeService;
        }

        public async Task<PanelCopyResult> CopyPanelAsync(CopyPanelViewModel model)
        {
            var sourcePanel = await _context.Panels
                .FirstOrDefaultAsync(p => p.PanelID == model.SourcePanelID);

            if (sourcePanel == null)
            {
                return new PanelCopyResult
                {
                    Success = false,
                    Message = "Ο αρχικός πίνακας δεν βρέθηκε."
                };
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerID == model.CustomerID && c.Active);

            if (customer == null)
            {
                return new PanelCopyResult
                {
                    Success = false,
                    Message = "Ο επιλεγμένος πελάτης δεν βρέθηκε."
                };
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var nextPanelCode = await _panelCodeService.GetNextPanelCodeAsync();

                var codeExists = await _context.Panels.AnyAsync(p => p.PanelCode == nextPanelCode);

                if (codeExists)
                {
                    return new PanelCopyResult
                    {
                        Success = false,
                        Message = "Δεν ήταν δυνατή η δημιουργία νέου κωδικού πίνακα. Προσπάθησε ξανά."
                    };
                }

                var newPanel = new Panel
                {
                    PanelCode = nextPanelCode,
                    CustomerID = customer.CustomerID,
                    CustomerName = customer.CustomerName,
                    Description = model.Description,
                    Status = PanelStatuses.UnderConstruction,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                };

                _context.Panels.Add(newPanel);
                await _context.SaveChangesAsync();

                if (model.CopyMaterials)
                {
                    var sourceMaterials = await _context.PanelMaterials
                        .Where(pm => pm.PanelID == model.SourcePanelID)
                        .OrderBy(pm => pm.PanelMaterialID)
                        .ToListAsync();

                    foreach (var item in sourceMaterials)
                    {
                        var newItem = new PanelMaterial
                        {
                            PanelID = newPanel.PanelID,
                            MaterialID = item.MaterialID,
                            SupplierID = item.SupplierID,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            DiscountPercent = model.CopyDiscounts ? item.DiscountPercent : 0m,
                            IsManualPrice = model.CopyManualPrices ? item.IsManualPrice : false,
                            ManualPriceReason = model.CopyManualPrices ? item.ManualPriceReason : null,
                            DateAdded = DateTime.Now,
                            LastModifiedDate = DateTime.Now,
                            AddedByUserID = item.AddedByUserID
                        };

                        _context.PanelMaterials.Add(newItem);
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return new PanelCopyResult
                {
                    Success = true,
                    NewPanelID = newPanel.PanelID,
                    NewPanelCode = nextPanelCode,
                    Message = $"Δημιουργήθηκε νέο αντίγραφο του πίνακα {sourcePanel.PanelCode} ως {nextPanelCode}."
                };
            }
            catch
            {
                await transaction.RollbackAsync();

                return new PanelCopyResult
                {
                    Success = false,
                    Message = "Παρουσιάστηκε σφάλμα κατά την αντιγραφή του πίνακα."
                };
            }
        }
    }
}