using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using panelapp.Constants;
using panelapp.Data;
using panelapp.Models;

namespace panelapp.Services
{
    public class MaterialService : IMaterialService
    {
        private readonly ApplicationDbContext _context;

        public MaterialService(ApplicationDbContext context)
        {
            _context = context;
        }
        public void NormalizeMaterial(Material model)
        {
            model.MaterialCode = (model.MaterialCode ?? string.Empty).Trim().ToUpperInvariant();
            model.Description = (model.Description ?? string.Empty).Trim();
        }

        public bool ValidateMaterialUnit(Material model, ModelStateDictionary modelState)
        {
            var normalizedUnit = MaterialUnits.NormalizeUnit(model.Unit);

            if (normalizedUnit == null)
            {
                modelState.AddModelError(nameof(model.Unit), "Η μονάδα πρέπει να είναι pcs ή meters.");
                return false;
            }

            model.Unit = normalizedUnit;
            return true;
        }

        public async Task<bool> MaterialCodeExistsForSupplierAsync(
            int supplierId,
            string materialCode,
            int? excludeMaterialId = null)
        {
            return await _context.Materials.AnyAsync(m =>
                m.SupplierID == supplierId &&
                m.MaterialCode == materialCode &&
                (!excludeMaterialId.HasValue || m.MaterialID != excludeMaterialId.Value));
        }

    }
}