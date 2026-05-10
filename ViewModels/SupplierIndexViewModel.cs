using panelapp.Models;

namespace panelapp.ViewModels
{
    public class SupplierIndexViewModel
    {
        public List<Supplier> Suppliers { get; set; } = new();

        public string SearchTerm { get; set; } = string.Empty;
        public string StatusFilter { get; set; } = "all";

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
    }
}