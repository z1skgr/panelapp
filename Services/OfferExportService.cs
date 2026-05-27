using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using panelapp.Data;

namespace panelapp.Services
{
    public class OfferExportService : IOfferExportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public OfferExportService(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<byte[]> ExportCustomerOfferExcelAsync(int offerId)
        {
            var offer = await _context.Offers
                .Include(x => x.OfferMaterials)
                    .ThenInclude(x => x.Material)
                .Include(x => x.OfferCabinets)
                    .ThenInclude(x => x.Cabinet)
                .Include(x => x.OfferExtraItems)
                .FirstOrDefaultAsync(x => x.OfferID == offerId);

            if (offer == null)
                return Array.Empty<byte>();

            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("Προσφορά Πελάτη");

            var logoPath = Path.Combine(
                _environment.WebRootPath,
                "images",
                "output-onlinepngtools.png"
            );

            if (File.Exists(logoPath))
            {
                ws.AddPicture(logoPath)
                    .MoveTo(ws.Cell("A1"))
                    .WithSize(120, 60);
            }

            ws.Cell("A4").Value = "ΠΡΟΣΦΟΡΑ";
            ws.Range("A4:C4").Merge();
            ws.Cell("A4").Style.Font.Bold = true;
            ws.Cell("A4").Style.Font.FontSize = 18;
            ws.Cell("A4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("A4").Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

            ws.Cell("A6").Value = "Κωδικός Προσφοράς";
            ws.Cell("B6").Value = offer.OfferCode;

            ws.Cell("A7").Value = "Πελάτης";
            ws.Cell("B7").Value = offer.CustomerName;

            ws.Cell("A8").Value = "Περιγραφή";
            ws.Cell("B8").Value = offer.Description ?? "";

            ws.Cell("A9").Value = "Ημερομηνία";
            ws.Cell("B9").Value = DateTime.Now;
            ws.Cell("B9").Style.DateFormat.Format = "dd/MM/yyyy";

            ws.Range("A6:A9").Style.Font.Bold = true;
            ws.Range("A6:A9").Style.Fill.BackgroundColor = XLColor.AliceBlue;

            var row = 12;


            var header = ws.Range(row, 1, row, 3);
            header.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            header.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            row++;

            row = WriteCustomerSection(ws, row, "ΥΛΙΚΑ", offer.OfferMaterials.Select(x => new
            {
                Description = x.Material?.Description ?? x.Material?.MaterialCode ?? "",
                Unit = "pcs",
                Quantity = x.Quantity
            }));

            row = WriteCustomerSection(ws, row, "ΕΡΜΑΡΙΑ", offer.OfferCabinets.Select(x => new
            {
                Description = x.Cabinet?.Description ?? x.Cabinet?.CabinetCode ?? "",
                Unit = "pcs",
                Quantity = x.Quantity
            }));

            row = WriteCustomerSection(ws, row, "ΛΟΙΠΑ ΥΛΙΚΑ", offer.OfferExtraItems.Select(x => new
            {
                Description = x.Description,
                Unit = x.Unit,
                Quantity = x.Quantity
            }));
            var dataEndRow = row - 1;

            if (dataEndRow >= 13)
            {
                ws.Range(13, 1, dataEndRow, 3)
                    .Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            }

            row += 2;

            var finalTotal =
                offer.OfferMaterials.Sum(x => x.OriginalTotalPrice)
                + offer.OfferCabinets.Sum(x => x.OriginalTotalPrice)
                + offer.OfferExtraItems.Sum(x => x.OriginalTotalPrice)
                + offer.LaborCost
                + offer.ProfitAmount;

            ws.Cell(row, 2).Value = "Σύνολο Προσφοράς";
            ws.Cell(row, 3).Value = finalTotal;

            ws.Range(row, 2, row, 3).Style.Font.Bold = true;
            ws.Range(row, 2, row, 3).Style.Fill.BackgroundColor = XLColor.LightYellow;

            ws.Column(1).Width = 55;
            ws.Column(2).Width = 12;
            ws.Column(3).Width = 14;

            ws.Column(1).Style.Alignment.WrapText = true;
            ws.Column(3).Style.NumberFormat.Format = "#,##0.00";

            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00 €";

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        private static int WriteCustomerSection(
            IXLWorksheet ws,
            int row,
            string title,
            IEnumerable<dynamic> items)
        {
            var list = items.ToList();

            if (!list.Any())
                return row;

            ws.Cell(row, 1).Value = title;
            ws.Range(row, 1, row, 3).Merge();
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;

            row++;

            ws.Cell(row, 1).Value = "Περιγραφή";
            ws.Cell(row, 2).Value = "Μονάδα";
            ws.Cell(row, 3).Value = "Ποσότητα";

            var header = ws.Range(row, 1, row, 3);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.AliceBlue;

            row++;

            foreach (var item in list)
            {
                ws.Cell(row, 1).Value = item.Description;
                ws.Cell(row, 2).Value = item.Unit;
                ws.Cell(row, 3).Value = item.Quantity;
                row++;
            }

            return row + 1;
        }


        public async Task<byte[]> ExportInternalCostingExcelAsync(int offerId)
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
            ws.Cell(headerRow, 8).Value = "Σύνολο ΝΕΤ";
            ws.Cell(headerRow, 9).Value = "Σύνολο Καταλόγου";

            var headerRange = ws.Range(headerRow, 1, headerRow, 9);
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
                ws.Cell(row, 9).Value = catalogTotal;
                row++;
            }

            row += 2;

            ws.Cell(row, 1).Value = "ΕΡΜΑΡΙΑ";
            ws.Range(row, 1, row, 9).Merge();
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
            ws.Cell(row, 8).Value = "Σύνολο ΝΕΤ";
            ws.Cell(row, 9).Value = "Σύνολο Καταλόγου";

            ws.Range(row, 1, row, 9).Style.Font.Bold = true;
            ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.LightGray;

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
                ws.Cell(row, 9).Value = catalogTotal;
                row++;
            }

            row += 2;

            ws.Cell(row, 1).Value = "ΛΟΙΠΑ ΥΛΙΚΑ";
            ws.Range(row, 1, row, 9).Merge();
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
            ws.Cell(row, 8).Value = "Σύνολο ΝΕΤ";
            ws.Cell(row, 9).Value = "Σύνολο Καταλόγου";

            ws.Range(row, 1, row, 9).Style.Font.Bold = true;
            ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.LightGray;

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
                ws.Cell(row, 9).Value = catalogTotal;
                row++;
            }

            int dataStartRow = headerRow + 1;
            int dataEndRow = row - 1;

            if (dataEndRow >= dataStartRow)
            {
                ws.Range(dataStartRow, 1, dataEndRow, 9)
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

            var materialsCatalogTotal = offer.OfferMaterials.Sum(x => x.OriginalTotalPrice);
            var cabinetsCatalogTotal = offer.OfferCabinets.Sum(x => x.OriginalTotalPrice);
            var extraItemsCatalogTotal = offer.OfferExtraItems.Sum(x => x.OriginalTotalPrice);

            var grandCatalogTotal =
                materialsCatalogTotal
                + cabinetsCatalogTotal
                + extraItemsCatalogTotal;



            ws.Cell(row, 6).Value = "Υλικά NET";
            ws.Cell(row, 7).Value = materialsNetTotal;

            row++;

            ws.Cell(row, 6).Value = "Ερμάρια NET";
            ws.Cell(row, 7).Value = cabinetsNetTotal;

            row++;

            ws.Cell(row, 6).Value = "Λοιπά Υλικά NET";
            ws.Cell(row, 7).Value = extraItemsNetTotal;

            row++;

            ws.Cell(row, 6).Value = "Εργατικά";
            ws.Cell(row, 7).Value = offer.LaborCost;

            row++;

            ws.Cell(row, 6).Value = "Κέρδος";
            ws.Cell(row, 7).Value = offer.ProfitAmount;

            row++;

            ws.Cell(row, 6).Value = "Τελική Προσφορά";
            ws.Cell(row, 7).Value = finalTotal;


            var summaryStartRow = row - 5;

            ws.Cell(summaryStartRow, 9).Value = "Υλικά Κατάλογος";
            ws.Cell(summaryStartRow, 10).Value = materialsCatalogTotal;

            ws.Cell(summaryStartRow + 1, 9).Value = "Ερμάρια Κατάλογος";
            ws.Cell(summaryStartRow + 1, 10).Value = cabinetsCatalogTotal;

            ws.Cell(summaryStartRow + 2, 9).Value = "Λοιπά Υλικά Κατάλογος";
            ws.Cell(summaryStartRow + 2, 10).Value = extraItemsCatalogTotal;

            ws.Cell(summaryStartRow + 3, 9).Value = "Εργατικά";
            ws.Cell(summaryStartRow + 3, 10).Value = offer.LaborCost;

            ws.Cell(summaryStartRow + 4, 9).Value = "Κέρδος";
            ws.Cell(summaryStartRow + 4, 10).Value = offer.ProfitAmount;

            ws.Cell(summaryStartRow + 5, 9).Value = "Σύνολο Καταλόγου";
            ws.Cell(summaryStartRow + 5, 10).Value = grandCatalogTotal;

            var netRange = ws.Range(summaryStartRow, 6, row, 7);
            netRange.Style.Font.Bold = true;
            netRange.Style.Fill.BackgroundColor = XLColor.LightYellow;
            netRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            netRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            var catalogRange = ws.Range(summaryStartRow, 9, summaryStartRow + 5, 10);
            catalogRange.Style.Font.Bold = true;
            catalogRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            catalogRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            catalogRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Column(4).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(5).Style.NumberFormat.Format = "#,##0.00 €";
            ws.Column(6).Style.NumberFormat.Format = "0.00%";
            ws.Column(7).Style.NumberFormat.Format = "#,##0.00 €";
            ws.Column(8).Style.NumberFormat.Format = "#,##0.00 €";
            ws.Column(9).Style.NumberFormat.Format = "#,##0.00 €";
            ws.Column(10).Style.NumberFormat.Format = "#,##0.00 €";



            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }
    }
}