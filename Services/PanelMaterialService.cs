using Microsoft.EntityFrameworkCore;
using panelapp.Constants;
using panelapp.Data;
using panelapp.Models;
using panelapp.ViewModels;

namespace panelapp.Services
{
    public class PanelMaterialService : IPanelMaterialService
    {
        private readonly ApplicationDbContext _context;

        public PanelMaterialService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, string? MaterialCode)> AddMaterialInlineAsync(AddMaterialToPanelViewModel model)
        {
            if (model.MaterialID <= 0)
            {
                return (false, "Δεν έγινε προσθήκη υλικού. Επίλεξε υλικό από τη λίστα.", null);
            }

            if (model.Quantity <= 0)
            {
                return (false, "Η ποσότητα πρέπει να είναι μεγαλύτερη από το μηδέν.", null);
            }

            if (model.DiscountPercent < 0 || model.DiscountPercent > 100)
            {
                return (false, "Η έκπτωση πρέπει να είναι από 0 έως 100.", null);
            }

            var material = await _context.Materials
                .FirstOrDefaultAsync(m => m.MaterialID == model.MaterialID && m.Active);

            if (material == null)
            {
                return (false, "Δεν βρέθηκε το επιλεγμένο υλικό.", null);
            }

            if (string.Equals(material.Unit, MaterialUnits.Pcs, StringComparison.OrdinalIgnoreCase) &&
                model.Quantity != Math.Floor(model.Quantity))
            {
                return (false, "Για υλικά σε Τεμάχια, η ποσότητα πρέπει να είναι θετικός ακέραιος.", material.MaterialCode);
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

            return (true, "Το υλικό προστέθηκε επιτυχώς.", material.MaterialCode);
        }



        public async Task<(bool Success, string Message, string? MaterialCode, decimal? Quantity)> RemoveMaterialAsync(int panelMaterialId)
        {
            var panelMaterial = await _context.PanelMaterials
                .FirstOrDefaultAsync(pm => pm.PanelMaterialID == panelMaterialId);

            if (panelMaterial == null)
            {
                return (false, "Το υλικό δεν βρέθηκε.", null, null);
            }

            var materialInfo = await (
                from pm in _context.PanelMaterials
                join m in _context.Materials on pm.MaterialID equals m.MaterialID
                where pm.PanelMaterialID == panelMaterialId
                select new
                {
                    m.MaterialCode,
                    pm.Quantity
                })
                .FirstOrDefaultAsync();

            _context.PanelMaterials.Remove(panelMaterial);

            return (
                true,
                "Το υλικό αφαιρέθηκε επιτυχώς.",
                materialInfo?.MaterialCode,
                materialInfo?.Quantity
            );
        }


        public async Task<(bool Success, string Message)> EditMaterialAsync(EditPanelMaterialViewModel model)
        {
            var panelMaterial = await _context.PanelMaterials
                .FirstOrDefaultAsync(pm => pm.PanelMaterialID == model.PanelMaterialID);

            if (panelMaterial == null)
            {
                return (false, "Το υλικό δεν βρέθηκε.");
            }

            panelMaterial.Quantity = model.Quantity;
            panelMaterial.DiscountPercent = model.DiscountPercent;
            panelMaterial.LastModifiedDate = DateTime.UtcNow;
            _context.Entry(panelMaterial)
                .Property(x => x.RowVersion)
                .OriginalValue = Convert.FromBase64String(model.RowVersion);

            return (true, "Το υλικό ενημερώθηκε επιτυχώς.");
        }


        public async Task<(bool Success, string Message)> EditMaterialAdminAsync(EditPanelMaterialAdminViewModel model)
        {
            var panelMaterial = await _context.PanelMaterials
                .FirstOrDefaultAsync(pm => pm.PanelMaterialID == model.PanelMaterialID);

            if (panelMaterial == null)
            {
                return (false, "Το υλικό δεν βρέθηκε.");
            }

            panelMaterial.Quantity = model.Quantity;
            panelMaterial.UnitPrice = model.UnitPrice;
            panelMaterial.DiscountPercent = model.DiscountPercent;
            panelMaterial.IsManualPrice = model.IsManualPrice;
            panelMaterial.ManualPriceReason = model.ManualPriceReason;
            panelMaterial.LastModifiedDate = DateTime.UtcNow;

            _context.Entry(panelMaterial)
                .Property(x => x.RowVersion)
                .OriginalValue = Convert.FromBase64String(model.RowVersion);

            return (true, "Το υλικό ενημερώθηκε επιτυχώς.");
        }
    }
}