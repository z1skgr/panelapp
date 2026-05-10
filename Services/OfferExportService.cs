using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using panelapp.Data;

namespace panelapp.Services
{
    public class OfferExportService : IOfferExportService
    {
        private readonly ApplicationDbContext _context;

        public OfferExportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> ExportExcelAsync(int offerId)
        {
            var offer = await _context.Offers
                .Include(x => x.OfferMaterials)
                    .ThenInclude(x => x.Material)
                .Include(x => x.OfferMaterials)
                    .ThenInclude(x => x.Supplier)
                .Include(x => x.OfferCabinets)
                    .ThenInclude(x => x.Cabinet)
                .Include(x => x.OfferCabinets)
                    .ThenInclude(x => x.Supplier)
                .Include(x => x.OfferExtraItems)
                .FirstOrDefaultAsync(x => x.OfferID == offerId);

            if (offer == null)
                return Array.Empty<byte>();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Προσφορά");

            var logoPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "images",
                "output-onlinepngtools.png"
            );

            if (File.Exists(logoPath))
            {
                var image = ws.AddPicture(logoPath)
                    .MoveTo(ws.Cell("A1"))
                    .WithSize(120, 60);
            }

            ws.Cell("A4").Value = "ΠΡΟΣΦΟΡΑ";
            ws.Range("A4:H4").Merge();
            ws.Cell("A4").Style.Font.Bold = true;
            ws.Cell("A4").Style.Font.FontSize = 16;
            ws.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("A4").Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

            ws.Cell("A6").Value = "Κωδικός Προσφοράς";
            ws.Cell("B6").Value = offer.OfferCode;

            ws.Cell("A7").Value = "Πελάτης";
            ws.Cell("B7").Value = offer.CustomerName;

            ws.Cell("A8").Value = "Περιγραφή";
            ws.Cell("B8").Value = offer.Description ?? "";

            ws.Cell("A9").Value = "Κατάσταση";
            ws.Cell("B9").Value = offer.Status;

            ws.Cell("A10").Value = "Ημερομηνία Export";
            ws.Cell("B10").Value = DateTime.Now;
            ws.Cell("B10").Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

            ws.Range("A6:A10").Style.Font.Bold = true;
            ws.Range("A6:A10").Style.Fill.BackgroundColor = XLColor.AliceBlue;

            int headerRow = 13;

            ws.Cell(headerRow, 1).Value = "Κωδικός";
            ws.Cell(headerRow, 2).Value = "Περιγραφή";
            ws.Cell(headerRow, 3).Value = "Προμηθευτής";
            ws.Cell(headerRow, 4).Value = "Ποσότητα";
            ws.Cell(headerRow, 5).Value = "Τιμή Μονάδας";
            ws.Cell(headerRow, 6).Value = "Έκπτωση %";
            ws.Cell(headerRow, 7).Value = "Έκπτωση Αξία";
            ws.Cell(headerRow, 8).Value = "ΤΙΜΗ ΝΕΤ";

            var headerRange = ws.Range(headerRow, 1, headerRow, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            int row = headerRow + 1;

            foreach (var item in offer.OfferMaterials)
            {
                var catalogTotal = item.Quantity * item.UnitPrice;
                var netTotal = item.LineNetTotal;
                var discountValue = catalogTotal - netTotal;

                ws.Cell(row, 1).Value = item.Material?.MaterialCode ?? "";
                ws.Cell(row, 2).Value = item.Material?.Description ?? "";
                ws.Cell(row, 3).Value = item.Supplier?.SupplierName ?? "";
                ws.Cell(row, 4).Value = item.Quantity;
                ws.Cell(row, 5).Value = item.UnitPrice;
                ws.Cell(row, 6).Value = item.DiscountPercent / 100m;
                ws.Cell(row, 7).Value = discountValue;
                ws.Cell(row, 8).Value = netTotal;

                row++;
            }

            row += 2;

            ws.Cell(row, 1).Value = "ΕΡΜΑΡΙΑ";
            ws.Range(row, 1, row, 8).Merge();
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;

            ws.Cell(row, 1).Value = "Κωδικός";
            ws.Cell(row, 2).Value = "Περιγραφή";
            ws.Cell(row, 3).Value = "Προμηθευτής";
            ws.Cell(row, 4).Value = "Ποσότητα";
            ws.Cell(row, 5).Value = "Τιμή Μονάδας";
            ws.Cell(row, 6).Value = "Έκπτωση %";
            ws.Cell(row, 7).Value = "Έκπτωση Αξία";
            ws.Cell(row, 8).Value = "ΤΙΜΗ ΝΕΤ";

            ws.Range(row, 1, row, 8).Style.Font.Bold = true;
            ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;

            foreach (var item in offer.OfferCabinets)
            {
                var catalogTotal = item.Quantity * item.UnitPrice;
                var netTotal = item.LineNetTotal;
                var discountValue = catalogTotal - netTotal;

                ws.Cell(row, 1).Value = item.Cabinet?.CabinetCode ?? "";
                ws.Cell(row, 2).Value = item.Cabinet?.Description ?? "";
                ws.Cell(row, 3).Value = item.Supplier?.SupplierName ?? "";
                ws.Cell(row, 4).Value = item.Quantity;
                ws.Cell(row, 5).Value = item.UnitPrice;
                ws.Cell(row, 6).Value = item.DiscountPercent / 100m;
                ws.Cell(row, 7).Value = discountValue;
                ws.Cell(row, 8).Value = netTotal;

                row++;
            }

            row += 2;

            ws.Cell(row, 1).Value = "ΛΟΙΠΑ ΥΛΙΚΑ";
            ws.Range(row, 1, row, 8).Merge();
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;

            ws.Cell(row, 1).Value = "Κωδικός";
            ws.Cell(row, 2).Value = "Περιγραφή";
            ws.Cell(row, 3).Value = "Μονάδα";
            ws.Cell(row, 4).Value = "Ποσότητα";
            ws.Cell(row, 5).Value = "Τιμή Μονάδας";
            ws.Cell(row, 6).Value = "Έκπτωση %";
            ws.Cell(row, 7).Value = "Έκπτωση Αξία";
            ws.Cell(row, 8).Value = "ΤΙΜΗ ΝΕΤ";

            ws.Range(row, 1, row, 8).Style.Font.Bold = true;
            ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;

            foreach (var item in offer.OfferExtraItems)
            {
                var catalogTotal = item.Quantity * item.UnitPrice;
                var netTotal = item.LineNetTotal;
                var discountValue = catalogTotal - netTotal;

                ws.Cell(row, 1).Value = item.ItemCode ?? "";
                ws.Cell(row, 2).Value = item.Description;
                ws.Cell(row, 3).Value = item.Unit;
                ws.Cell(row, 4).Value = item.Quantity;
                ws.Cell(row, 5).Value = item.UnitPrice;
                ws.Cell(row, 6).Value = item.DiscountPercent / 100m;
                ws.Cell(row, 7).Value = discountValue;
                ws.Cell(row, 8).Value = netTotal;

                row++;
            }

            int dataStartRow = headerRow + 1;
            int dataEndRow = row - 1;

            if (dataEndRow >= dataStartRow)
            {
                ws.Range(dataStartRow, 1, dataEndRow, 8)
                    .Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            }

            row += 1;

            var materialsNetTotal = offer.OfferMaterials.Sum(x => x.LineNetTotal);
            var cabinetsNetTotal = offer.OfferCabinets.Sum(x => x.LineNetTotal);
            var extraItemsNetTotal = offer.OfferExtraItems.Sum(x => x.LineNetTotal);
            var finalTotal =
                materialsNetTotal
                + cabinetsNetTotal
                + extraItemsNetTotal
                + offer.LaborCost
                + offer.ProfitAmount;

            ws.Cell(row, 7).Value = "Υλικά NET";
            ws.Cell(row, 8).Value = materialsNetTotal;

            row++;

            ws.Cell(row, 7).Value = "Ερμάρια NET";
            ws.Cell(row, 8).Value = cabinetsNetTotal;

            row++;

            ws.Cell(row, 7).Value = "Λοιπά Υλικά NET";
            ws.Cell(row, 8).Value = extraItemsNetTotal;

            row++;

            ws.Cell(row, 7).Value = "Εργατικά";
            ws.Cell(row, 8).Value = offer.LaborCost;

            row++;

            ws.Cell(row, 7).Value = "Κέρδος";
            ws.Cell(row, 8).Value = offer.ProfitAmount;

            row++;

            ws.Cell(row, 7).Value = "Τελική Προσφορά";
            ws.Cell(row, 8).Value = finalTotal;

            var totalsRange = ws.Range(row - 5, 7, row, 8);
            totalsRange.Style.Font.Bold = true;
            totalsRange.Style.Fill.BackgroundColor = XLColor.LightYellow;
            totalsRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            totalsRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;


            ws.Column(4).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(5).Style.NumberFormat.Format = "#,##0.00 €";
            ws.Column(6).Style.NumberFormat.Format = "0.00%";
            ws.Column(7).Style.NumberFormat.Format = "#,##0.00 €";
            ws.Column(8).Style.NumberFormat.Format = "#,##0.00 €";


            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }
    }
}