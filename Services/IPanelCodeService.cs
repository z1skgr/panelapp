namespace panelapp.Services
{
    public interface IPanelCodeService
    {
        Task<string> GetNextPanelCodeAsync();
    }
}