using panelapp.ViewModels;

namespace panelapp.Services
{
    public interface IDashboardService
    {
        Task<HomeDashboardViewModel> GetDashboardAsync();
    }
}