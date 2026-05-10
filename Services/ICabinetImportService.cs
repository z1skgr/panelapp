namespace panelapp.Services
{
    public interface ICabinetImportService
    {
        Task<MaterialImportResult> ImportAsync(int supplierId, IFormFile excelFile);
    }
}