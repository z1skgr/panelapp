using Microsoft.EntityFrameworkCore;
using panelapp.Data;

namespace panelapp.Services.AI
{
    public class OfferAiSummaryService : IOfferAiSummaryService
    {
        private readonly ApplicationDbContext _context;

        public OfferAiSummaryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateSummaryAsync(
            int offerId,
            CancellationToken cancellationToken = default)
        {
            var offer = await _context.Offers
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.OfferMaterials)
                    .ThenInclude(x => x.Material)
                .Include(x => x.OfferCabinets)
                    .ThenInclude(x => x.Cabinet)
                .Include(x => x.OfferExtraItems)
                .FirstOrDefaultAsync(x => x.OfferID == offerId, cancellationToken);

            if (offer == null)
                return "Η προσφορά δεν βρέθηκε.";

            var lines = new List<string>
            {
                $"Σύνοψη προσφοράς {offer.OfferCode}",
                $"Πελάτης: {offer.CustomerName}",
                ""
            };

            if (!string.IsNullOrWhiteSpace(offer.Description))
            {
                lines.Add($"Περιγραφή: {offer.Description}");
                lines.Add("");
            }

            if (offer.OfferMaterials.Any())
            {
                lines.Add("Υλικά:");
                foreach (var item in offer.OfferMaterials)
                {
                    lines.Add($"- {item.Material?.MaterialCode} {item.Material?.Description} x {item.Quantity:N2}");
                }

                lines.Add("");
            }

            if (offer.OfferCabinets.Any())
            {
                lines.Add("Ερμάρια:");
                foreach (var item in offer.OfferCabinets)
                {
                    lines.Add($"- {item.Cabinet?.CabinetCode} {item.Cabinet?.Description} x {item.Quantity:N2}");
                }

                lines.Add("");
            }

            if (offer.OfferExtraItems.Any())
            {
                lines.Add("Λοιπά:");
                foreach (var item in offer.OfferExtraItems)
                {
                    lines.Add($"- {item.Description} x {item.Quantity:N2}");
                }

                lines.Add("");
            }

            lines.Add($"Εργατικά: {offer.LaborCost:N2} €");
            lines.Add($"Κέρδος: {offer.ProfitAmount:N2} €");
            lines.Add($"Τελικό σύνολο: {offer.FinalOfferTotal:N2} €");

            return string.Join(Environment.NewLine, lines);
        }
    }
}