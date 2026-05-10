namespace panelapp.Models
{
    public class Panel
    {
        public int PanelID { get; set; }
        public string PanelCode { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public int? CustomerID { get; set; }
        public Customer? Customer { get; set; }

        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public List<PanelMaterial> PanelMaterials { get; set; } = new();
    }
}