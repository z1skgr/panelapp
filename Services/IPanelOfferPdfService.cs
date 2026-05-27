namespace panelapp.Services
{
    public interface IPanelOfferPdfService
    {
        Task<byte[]> GenerateCustomerOfferPdfAsync(int panelId);
        Task<byte[]> GenerateInternalCostingPdfAsync(int panelId);
    }
}