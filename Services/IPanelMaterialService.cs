using panelapp.ViewModels;

public interface IPanelMaterialService
{
    Task<(bool Success, string Message, string? MaterialCode)> AddMaterialInlineAsync(AddMaterialToPanelViewModel model);

    Task<(bool Success, string Message, string? MaterialCode, decimal? Quantity)> RemoveMaterialAsync(int panelMaterialId);

    Task<(bool Success, string Message)> EditMaterialAsync(EditPanelMaterialViewModel model);

    Task<(bool Success, string Message)> EditMaterialAdminAsync(EditPanelMaterialAdminViewModel model);
}