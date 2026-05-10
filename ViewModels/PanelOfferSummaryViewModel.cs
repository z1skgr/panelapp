namespace panelapp.ViewModels
{
    public class PanelOfferSummaryViewModel
    {
        public decimal MaterialsTotalWithoutDiscount { get; set; }
        public decimal MaterialsNetTotal { get; set; }

        public decimal CabinetsNetTotal { get; set; }

        public decimal ExtraItemsNetTotal { get; set; }
        public decimal LaborCost { get; set; }
        public decimal ProfitAmount { get; set; }
        public decimal FinalOfferTotal { get; set; }



    }
}