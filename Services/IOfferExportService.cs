namespace panelapp.Services
{
    public interface IOfferExportService
    {
        Task<byte[]> ExportExcelAsync(int offerId);
    }

}
