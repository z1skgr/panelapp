using panelapp.Services.Results;
using panelapp.ViewModels;

namespace panelapp.Services
{
    public interface IPanelService
    {
        Task<PanelCopyResult> CopyPanelAsync(CopyPanelViewModel model);
    }
}