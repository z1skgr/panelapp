using panelapp.Models;

namespace panelapp.ViewModels
{
    public class PanelIndexViewModel
    {
        public List<Panel> Panels { get; set; } = new();

        public string SearchTerm { get; set; } = string.Empty;

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
    }
}