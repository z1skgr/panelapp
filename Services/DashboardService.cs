using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using panelapp.Constants;
using panelapp.Data;
using panelapp.ViewModels;

namespace panelapp.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<HomeDashboardViewModel> GetDashboardAsync()
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var totalPanels = await _context.Panels.CountAsync();

            var underConstructionPanels =
                await _context.Panels.CountAsync(p =>
                    p.Status == PanelStatuses.UnderConstruction);

            var completedPanels =
                await _context.Panels.CountAsync(p =>
                    p.Status == PanelStatuses.Completed);

            var cancelledPanels =
                await _context.Panels.CountAsync(p =>
                    p.Status == PanelStatuses.Cancelled);

            var topCustomer = await _context.Panels
                .AsNoTracking()
                .Where(p => p.CustomerName != null && p.CustomerName != "")
                .GroupBy(p => p.CustomerName!)
                .Select(g => new
                {
                    CustomerName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            var recentPanels = await _context.Panels
                .AsNoTracking()
                .OrderByDescending(p => p.LastModifiedDate)
                .Take(8)
                .Select(p => new RecentPanelRow
                {
                    PanelID = p.PanelID,
                    PanelCode = p.PanelCode,
                    CustomerName = p.CustomerName ?? string.Empty,
                    Description = p.Description,
                    Status = p.Status,
                    LastModifiedDate = p.LastModifiedDate
                })
                .ToListAsync();

            var activityFeed = await _context.ActivityLogs
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .Select(a => new ActivityFeedRow
                {
                    PanelID = a.EntityType == "Panel" ? a.EntityID : null,
                    Title = a.Title,
                    Description = string.IsNullOrWhiteSpace(a.Description)
                        ? (a.UserName ?? string.Empty)
                        : a.Description,
                    Icon = GetActivityIcon(a.EntityType, a.ActionType),
                    BadgeClass = GetActivityBadgeClass(a.EntityType, a.ActionType),
                    CreatedAt = a.CreatedAt,
                    EntityType = a.EntityType,
                    EntityID = a.EntityID
                })
                .ToListAsync();

            var groupedPanels = await _context.Panels
                .AsNoTracking()
                .Where(p => p.CustomerName != null && p.CustomerName != "")
                .GroupBy(p => new { p.CustomerName, p.Status })
                .Select(g => new
                {
                    Customer = g.Key.CustomerName!,
                    Status = g.Key.Status,
                    Count = g.Count()
                })
                .ToListAsync();

            var orderedCustomers = groupedPanels
                .GroupBy(x => x.Customer)
                .OrderByDescending(g => g.Sum(x => x.Count))
                .Select(g => g.Key)
                .ToList();

            var allCustomersData = orderedCustomers.Select(c => new
            {
                customer = c,
                underConstruction = groupedPanels
                    .Where(x => x.Customer == c && x.Status == PanelStatuses.UnderConstruction)
                    .Sum(x => x.Count),

                completed = groupedPanels
                    .Where(x => x.Customer == c && x.Status == PanelStatuses.Completed)
                    .Sum(x => x.Count),

                cancelled = groupedPanels
                    .Where(x => x.Customer == c && x.Status == PanelStatuses.Cancelled)
                    .Sum(x => x.Count)
            }).ToList();

            var totalOffers = await _context.Offers.CountAsync();

            var draftOffers = await _context.Offers.CountAsync(x => x.Status == "Draft");
            var sentOffers = await _context.Offers.CountAsync(x => x.Status == "Sent");
            var acceptedOffers = await _context.Offers.CountAsync(x => x.Status == "Accepted");
            var convertedOffers = await _context.Offers.CountAsync(x => x.Status == "Converted");
            var rejectedOffers = await _context.Offers.CountAsync(x => x.Status == "Rejected");

            var offersThisMonth = await _context.Offers
                .CountAsync(x => x.CreatedDate >= monthStart);

            var acceptedOfferProfit = await _context.Offers
                .Where(x => x.Status == "Accepted" || x.Status == "Converted")
                .SumAsync(x => x.ProfitAmount);

            var acceptedOfferLabor = await _context.Offers
                .Where(x => x.Status == "Accepted")
                .SumAsync(x => x.LaborCost + x.ProfitAmount);

            var convertedOfferLabor = await _context.Offers
                .Where(x => x.Status == "Converted")
                .SumAsync(x => x.LaborCost + x.ProfitAmount);

            var estimatedAcceptedOfferValue = acceptedOfferLabor;
            var estimatedConvertedOfferValue = convertedOfferLabor;

            var offerAcceptanceRate =
                totalOffers == 0
                    ? 0
                    : Math.Round((decimal)acceptedOffers / totalOffers * 100, 1);

            var offerConversionRate =
                acceptedOffers == 0
                    ? 0
                    : Math.Round((decimal)convertedOffers / acceptedOffers * 100, 1);

            var model = new HomeDashboardViewModel
            {
                TotalPanels = totalPanels,
                UnderConstructionPanels = underConstructionPanels,
                CompletedPanels = completedPanels,
                CancelledPanels = cancelledPanels,

                TotalMaterials = await _context.Materials.CountAsync(),
                ActiveMaterials = await _context.Materials.CountAsync(m => m.Active),

                TotalSuppliers = await _context.Suppliers.CountAsync(),
                ActiveSuppliers = await _context.Suppliers.CountAsync(s => s.Active),
                InactiveSuppliers = await _context.Suppliers.CountAsync(s => !s.Active),

                TotalCustomers = await _context.Customers.CountAsync(),

                CompletionRate = totalPanels == 0
                    ? 0
                    : Math.Round((decimal)completedPanels / totalPanels * 100, 1),

                UnderConstructionRate = totalPanels == 0
                    ? 0
                    : Math.Round((decimal)underConstructionPanels / totalPanels * 100, 1),

                TopCustomerName = topCustomer?.CustomerName ?? "-",
                TopCustomerPanelCount = topCustomer?.Count ?? 0,

                PanelsThisMonth =
                    await _context.Panels.CountAsync(p =>
                        p.CreatedDate >= monthStart),

                LastPanelUpdate = recentPanels.FirstOrDefault()?.LastModifiedDate,

                RecentPanels = recentPanels,
                ActivityFeed = activityFeed,

                ChartDataJson = JsonConvert.SerializeObject(allCustomersData),

                TotalOffers = totalOffers,
                DraftOffers = draftOffers,
                SentOffers = sentOffers,
                AcceptedOffers = acceptedOffers,
                ConvertedOffers = convertedOffers,
                RejectedOffers = rejectedOffers,

                OffersThisMonth = offersThisMonth,

                EstimatedAcceptedOfferValue = estimatedAcceptedOfferValue,
                EstimatedConvertedOfferValue = estimatedConvertedOfferValue,
                EstimatedOfferProfit = acceptedOfferProfit,

                TotalCabinets = await _context.Cabinets.CountAsync(),
                ActiveCabinets = await _context.Cabinets.CountAsync(x => x.Active),

                OfferAcceptanceRate = offerAcceptanceRate,
                OfferConversionRate = offerConversionRate


            };

            return model;
        }

        private static string GetActivityIcon(string entityType, string actionType)
        {
            return (entityType, actionType) switch
            {
                ("Panel", "Created") => "bi-plus-circle",
                ("Panel", "Updated") => "bi-pencil-square",
                ("Panel", "Deleted") => "bi-trash",
                ("Material", "Created") => "bi-box-seam",
                ("Material", "Updated") => "bi-pencil-square",
                ("Customer", "Created") => "bi-person-plus",
                ("Supplier", "Created") => "bi-building-add",
                ("Import", "Imported") => "bi-file-earmark-excel",
                ("Offer", "Created") => "bi-file-earmark-plus",
                ("Offer", "Updated") => "bi-file-earmark-text",
                ("Offer", "Converted") => "bi-arrow-right-circle",
                ("Offer", "Exported") => "bi-download",
                ("Cabinet", "Created") => "bi-box-seam",
                ("Cabinet", "Updated") => "bi-pencil-square",
                ("Panel", "Exported") => "bi-download",
                _ => "bi-clock-history"
            };
        }

        private static string GetActivityBadgeClass(string entityType, string actionType)
        {
            return actionType switch
            {
                "Created" => "bg-success",
                "Updated" => "bg-primary",
                "Deleted" => "bg-danger",
                "Imported" => "bg-info text-dark",
                "Converted" => "bg-warning text-dark",
                "Exported" => "bg-info text-dark",
                _ => "bg-secondary"
            };
        }
    }
}