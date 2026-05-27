using Microsoft.EntityFrameworkCore;
using panelapp.Data;


namespace panelapp.Services
{
    public class OfferCodeService : IOfferCodeService
    {
        private readonly ApplicationDbContext _context;

        public OfferCodeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateNextOfferCodeAsync()
        {
            var currentYear = DateTime.Now.Year;

            var lastOffer = await _context.Offers
                .Where(x => x.OfferCode.StartsWith($"OFF-{currentYear}-"))
                .OrderByDescending(x => x.OfferID)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastOffer != null)
            {
                var parts = lastOffer.OfferCode.Split('-');

                if (parts.Length == 3 &&
                    int.TryParse(parts[2], out int parsed))
                {
                    nextNumber = parsed + 1;
                }
            }

            return $"OFF-{currentYear}-{nextNumber:D4}";
        }
    }
}