using Microsoft.EntityFrameworkCore;
using panelapp.Data;
using panelapp.Models.AI;

namespace panelapp.Services.AI
{
    public class OfferAiOperationExecutor : IOfferAiOperationExecutor
    {
        private readonly ApplicationDbContext _context;

        public OfferAiOperationExecutor(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> ExecuteAsync(
            int offerId,
            OfferAiOperation operation,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(operation.OperationType))
                return "Δεν αναγνωρίστηκε ενέργεια.";

            return operation.OperationType switch
            {
                "update_quantity" => await UpdateQuantityAsync(offerId, operation, cancellationToken),
                "update_discount" => await UpdateDiscountAsync(offerId, operation, cancellationToken),
                "remove_item" => await RemoveItemAsync(offerId, operation, cancellationToken),
                _ => "Η ενέργεια δεν υποστηρίζεται ακόμα."
            };
        }

        private async Task<string> UpdateQuantityAsync(
            int offerId,
            OfferAiOperation operation,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(operation.TargetItem) || operation.Quantity == null)
                return "Λείπει είδος ή ποσότητα.";

            var target = operation.TargetItem.Trim();
            var materials = await _context.OfferMaterials
                .Include(x => x.Material)
                .Include(x => x.Offer)
                .Where(x => x.OfferID == offerId)
                .ToListAsync(cancellationToken);

            var normalizedTarget = NormalizeCode(target);

            var material = materials.FirstOrDefault(x =>
                 x.Material != null &&
                 (
                     NormalizeCode(x.Material.MaterialCode).Contains(normalizedTarget) ||
                     normalizedTarget.Contains(NormalizeCode(x.Material.MaterialCode)) ||
                     x.Material.Description.Contains(target, StringComparison.OrdinalIgnoreCase)
                 ));


            if (material != null)
            {
                material.Quantity = operation.Quantity.Value;
                material.LastModifiedDate = DateTime.Now;

                if (material.Offer != null)
                    material.Offer.LastModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync(cancellationToken);

                return $"Ενημερώθηκε η ποσότητα του υλικού {material.Material?.MaterialCode} σε {operation.Quantity.Value:N2}.";
            }

            var cabinet = await _context.OfferCabinets
                .Include(x => x.Cabinet)
                .Include(x => x.Offer)
                .FirstOrDefaultAsync(x =>
                    x.OfferID == offerId &&
                    x.Cabinet != null &&
                    (
                        x.Cabinet.CabinetCode.Contains(target) ||
                        x.Cabinet.Description.Contains(target)
                    ),
                    cancellationToken);

            if (cabinet != null)
            {
                cabinet.Quantity = operation.Quantity.Value;
                cabinet.LastModifiedDate = DateTime.Now;

                if (cabinet.Offer != null)
                    cabinet.Offer.LastModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync(cancellationToken);

                return $"Ενημερώθηκε η ποσότητα του ερμαρίου {cabinet.Cabinet?.CabinetCode} σε {operation.Quantity.Value:N2}.";
            }

            return "Δεν βρέθηκε υλικό ή ερμάριο που να ταιριάζει.";
        }

        private async Task<string> UpdateDiscountAsync(
            int offerId,
            OfferAiOperation operation,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(operation.TargetItem) || operation.DiscountPercent == null)
                return "Λείπει είδος ή έκπτωση.";

            if (operation.DiscountPercent < 0 || operation.DiscountPercent > 100)
                return "Η έκπτωση πρέπει να είναι από 0 έως 100.";

            var target = operation.TargetItem.Trim();

            var materials = await _context.OfferMaterials
                .Include(x => x.Material)
                .Include(x => x.Supplier)
                .Include(x => x.Offer)
                .Where(x =>
                    x.OfferID == offerId &&
                    (
                        (x.Material != null &&
                            (
                                x.Material.MaterialCode.Contains(target) ||
                                x.Material.Description.Contains(target)
                            )) ||
                        (x.Supplier != null && x.Supplier.SupplierName.Contains(target))
                    ))
                .ToListAsync(cancellationToken);

            var cabinets = await _context.OfferCabinets
                .Include(x => x.Cabinet)
                .Include(x => x.Supplier)
                .Include(x => x.Offer)
                .Where(x =>
                    x.OfferID == offerId &&
                    (
                        (x.Cabinet != null &&
                            (
                                x.Cabinet.CabinetCode.Contains(target) ||
                                x.Cabinet.Description.Contains(target)
                            )) ||
                        (x.Supplier != null && x.Supplier.SupplierName.Contains(target))
                    ))
                .ToListAsync(cancellationToken);

            var affected = 0;

            foreach (var item in materials)
            {
                item.DiscountPercent = operation.DiscountPercent.Value;
                item.LastModifiedDate = DateTime.Now;

                if (item.Offer != null)
                    item.Offer.LastModifiedDate = DateTime.Now;

                affected++;
            }

            foreach (var item in cabinets)
            {
                item.DiscountPercent = operation.DiscountPercent.Value;
                item.LastModifiedDate = DateTime.Now;

                if (item.Offer != null)
                    item.Offer.LastModifiedDate = DateTime.Now;

                affected++;
            }

            if (affected == 0)
                return "Δεν βρέθηκαν γραμμές που να ταιριάζουν για αλλαγή έκπτωσης.";

            await _context.SaveChangesAsync(cancellationToken);

            return $"Ενημερώθηκε έκπτωση {operation.DiscountPercent.Value:N2}% σε {affected} γραμμές.";
        }

        private async Task<string> RemoveItemAsync(
            int offerId,
            OfferAiOperation operation,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(operation.TargetItem))
                return "Λείπει το είδος για αφαίρεση.";

            var target = operation.TargetItem.Trim();

            var material = await _context.OfferMaterials
                .Include(x => x.Material)
                .Include(x => x.Offer)
                .FirstOrDefaultAsync(x =>
                    x.OfferID == offerId &&
                    x.Material != null &&
                    (
                        x.Material.MaterialCode.Contains(target) ||
                        x.Material.Description.Contains(target)
                    ),
                    cancellationToken);

            if (material != null)
            {
                var code = material.Material?.MaterialCode ?? "υλικό";

                if (material.Offer != null)
                    material.Offer.LastModifiedDate = DateTime.Now;

                _context.OfferMaterials.Remove(material);
                await _context.SaveChangesAsync(cancellationToken);

                return $"Αφαιρέθηκε το υλικό {code}.";
            }

            var cabinet = await _context.OfferCabinets
                .Include(x => x.Cabinet)
                .Include(x => x.Offer)
                .FirstOrDefaultAsync(x =>
                    x.OfferID == offerId &&
                    x.Cabinet != null &&
                    (
                        x.Cabinet.CabinetCode.Contains(target) ||
                        x.Cabinet.Description.Contains(target)
                    ),
                    cancellationToken);

            if (cabinet != null)
            {
                var code = cabinet.Cabinet?.CabinetCode ?? "ερμάριο";

                if (cabinet.Offer != null)
                    cabinet.Offer.LastModifiedDate = DateTime.Now;

                _context.OfferCabinets.Remove(cabinet);
                await _context.SaveChangesAsync(cancellationToken);

                return $"Αφαιρέθηκε το ερμάριο {code}.";
            }

            return "Δεν βρέθηκε υλικό ή ερμάριο για αφαίρεση.";
        }


        private static string NormalizeCode(string value)
        {
            return value
                .Trim()
                .Replace(" ", "")
                .Replace("-", "")
                .ToUpperInvariant();
        }
    }
}