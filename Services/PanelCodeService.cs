using Microsoft.EntityFrameworkCore;
using panelapp.Data;

namespace panelapp.Services
{
    public class PanelCodeService : IPanelCodeService
    {
        private readonly ApplicationDbContext _context;

        public PanelCodeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetNextPanelCodeAsync()
        {
            var panelCodes = await _context.Panels
                .Select(p => p.PanelCode)
                .ToListAsync();

            int maxNumber = 0;

            foreach (var code in panelCodes)
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    continue;
                }

                var digits = new string(code.Where(char.IsDigit).ToArray());

                if (int.TryParse(digits, out int number) && number > maxNumber)
                {
                    maxNumber = number;
                }
            }

            return $"ZL-{(maxNumber + 1):D3}";
        }
    }
}
