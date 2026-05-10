namespace panelapp.Services
{
    public interface IOfferCodeService
    {
        Task<string> GenerateNextOfferCodeAsync();
    }
}