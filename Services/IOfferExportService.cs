namespace panelapp.Services
{
    public interface IOfferExportService
    {
        Task<byte[]> ExportInternalCostingExcelAsync(int offerId);

        Task<byte[]> ExportCustomerOfferExcelAsync(int offerId);
    }

}
