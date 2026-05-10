namespace panelapp.Services
{
    public interface IPanelExportService
    {
        Task<byte[]> ExportCsvAsync(int panelId);
        Task<byte[]> ExportExcelAsync(int panelId);
    }
}