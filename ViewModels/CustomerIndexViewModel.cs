using panelapp.Models;

namespace panelapp.ViewModels
{
    public class CustomerIndexViewModel
    {
        public List<Customer> Customers { get; set; } = new();

        public string SearchTerm { get; set; } = string.Empty;

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        public int CustomerPageSize { get; set; }
        public int PanelsPerCustomer { get; set; }

        public int? ExpandedCustomerId { get; set; }
    }
}