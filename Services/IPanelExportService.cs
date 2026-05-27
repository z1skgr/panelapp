namespace panelapp.Services
{
    public interface IPanelExportService
    {
        Task<byte[]> ExportInternalCostingCsvAsync(int panelId);

        Task<byte[]> ExportInternalCostingExcelAsync(int panelId);

        Task<byte[]> ExportCustomerOfferExcelAsync(int panelId);
    }
}