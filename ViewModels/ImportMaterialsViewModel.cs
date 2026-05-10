using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace panelapp.ViewModels
{
    public class ImportMaterialsViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Επιλέξτε Προμηθευτή.")]
        [Display(Name = "Προμηθευτής")]
        public int SupplierID { get; set; }

        [Required(ErrorMessage = "Επιλέξτε αρχείο Excel.")]
        [Display(Name = "Αρχειο Excel")]
        public IFormFile? ExcelFile { get; set; }

        public List<SelectListItem> Suppliers { get; set; } = new();

        public int InsertedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int SkippedCount { get; set; }

        public List<string> Messages { get; set; } = new();

        public string ImportType { get; set; } = "Material";
    }
}