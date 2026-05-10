namespace panelapp.Services
{
    public interface IOfferPdfService
    {
        Task<byte[]> GenerateCustomerOfferPdfAsync(int offerId);
    }
}