using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using panelapp.Data;
using System.Globalization;
using System.Text;

namespace panelapp.Services
{
    public class PanelExportService : IPanelExportService
    {
        private readonly ApplicationDbContext _context;

        public PanelExportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> ExportCsvAsync(int panelId)
        {
            var panel = await _context.Panels
                .Include(p => p.PanelCabinets)
                    .ThenInclude(x => x.Cabinet)
                .Include(p => p.PanelCabinets)
                    .ThenInclude(x => x.Supplier)
                .Include(p => p.PanelExtraItems)
                .FirstOrDefaultAsync(p => p.PanelID == panelId);

            if (panel == null)
                return Array.Empty<byte>();

            var materialRows = await GetPanelExportRowsAsync(panelId);

            var csv = new StringBuilder();

            csv.AppendLine("Κατηγορία,Κωδικός,Περιγραφή,Προμηθευτής,Μονάδα,Ποσότητα,Τιμή Μονάδας,ΤΙΜΗ ΚΑΤ,Έκπτωση %,Έκπτωση Αξία,ΤΙΜΗ ΝΕΤ");

            decimal materialsNetTotal = 0m;
            decimal cabinetsNetTotal = 0m;
            decimal extraItemsNetTotal = 0m;

            void AppendRow(
                string category,
                string code,
                string description,
                string supplier,
                string unit,
                decimal quantity,
                decimal unitPrice,
                decimal discountPercent)
            {
                var catalogTotal = quantity * unitPrice;
                var netTotal = catalogTotal * (1 - discountPercent / 100m);
                var discountValue = catalogTotal - netTotal;

                csv.AppendLine(string.Join(",",
                    CsvEscape(category),
                    CsvEscape(code),
                    CsvEscape(description),
                    CsvEscape(supplier),
                    CsvEscape(unit),
                    quantity.ToString("0.00", CultureInfo.InvariantCulture),
                    unitPrice.ToString("0.00", CultureInfo.InvariantCulture),
                    catalogTotal.ToString("0.00", CultureInfo.InvariantCulture),
                    discountPercent.ToString("0.00", CultureInfo.InvariantCulture),
                    discountValue.ToString("0.00", CultureInfo.InvariantCulture),
                    netTotal.ToString("0.00", CultureInfo.InvariantCulture)
                ));
            }

            foreach (var item in materialRows)
            {
                AppendRow(
                    "Ηλεκτρολογικά Υλικά",
                    item.MaterialCode,
                    item.Description,
                    item.Supplier,
                    item.Unit,
                    item.Quantity,
                    item.UnitPrice,
                    item.DiscountPercent);

                materialsNetTotal += item.NetTotal;
            }

            foreach (var item in panel.PanelCabinets)
            {
                AppendRow(
                    "Ερμάρια",
                    item.Cabinet?.CabinetCode ?? "",
                    item.Cabinet?.Description ?? "",
                    item.Supplier?.SupplierName ?? "",
                    item.Cabinet?.Unit ?? "pcs",
                    item.Quantity,
                    item.UnitPrice,
                    item.DiscountPercent);

                cabinetsNetTotal += item.LineNetTotal;
            }

            foreach (var item in panel.PanelExtraItems)
            {
                AppendRow(
                    "Λοιπά Υλικά",
                    item.ItemCode ?? "",
                    item.Description,
                    "",
                    item.Unit,
                    item.Quantity,
                    item.UnitPrice,
                    item.DiscountPercent);

                extraItemsNetTotal += item.LineNetTotal;
            }

            var netTotal = materialsNetTotal + cabinetsNetTotal + extraItemsNetTotal;
            var finalTotal = netTotal + panel.LaborCost + panel.ProfitAmount;

            csv.AppendLine();
            csv.AppendLine($"ΥΛΙΚΑ NET,,,,,,,,,,{materialsNetTotal.ToString("0.00", CultureInfo.InvariantCulture)}");
            csv.AppendLine($"ΕΡΜΑΡΙΑ NET,,,,,,,,,,{cabinetsNetTotal.ToString("0.00", CultureInfo.InvariantCulture)}");
            csv.AppendLine($"ΛΟΙΠΑ ΥΛΙΚΑ NET,,,,,,,,,,{extraItemsNetTotal.ToString("0.00", CultureInfo.InvariantCulture)}");
            csv.AppendLine($"ΕΡΓΑΤΙΚΑ,,,,,,,,,,{panel.LaborCost.ToString("0.00", CultureInfo.InvariantCulture)}");
            csv.AppendLine($"ΚΕΡΔΟΣ,,,,,,,,,,{panel.ProfitAmount.ToString("0.00", CultureInfo.InvariantCulture)}");
            csv.AppendLine($"ΣΥΝΟΛΟ ΚΟΣΤΟΛΟΓΗΣΗΣ,,,,,,,,,,{finalTotal.ToString("0.00", CultureInfo.InvariantCulture)}");

            var preamble = Encoding.UTF8.GetPreamble();
            var content = Encoding.UTF8.GetBytes(csv.ToString());

            return preamble.Concat(content).ToArray();
        }

        public async Task<byte[]> ExportExcelAsync(int panelId)
        {
            var panel = await _context.Panels
                .Include(p => p.PanelCabinets)
                    .ThenInclude(x => x.Cabinet)
                .Include(p => p.PanelCabinets)
                    .ThenInclude(x => x.Supplier)
                .Include(p => p.PanelExtraItems)
                .FirstOrDefaultAsync(p => p.PanelID == panelId);

            if (panel == null)
                return Array.Empty<byte>();

            var materialRows = await GetPanelExportRowsAsync(panelId);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Panel Export");

            ws.Cell("A1").Value = "EXPORT ΠΙΝΑΚΑ";
            ws.Range("A1:J1").Merge();
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;
            ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

            ws.Cell("A3").Value = "Κωδικός Πίνακα";
            ws.Cell("B3").Value = panel.PanelCode;

            ws.Cell("A4").Value = "Πελάτης";
            ws.Cell("B4").Value = panel.CustomerName ?? "";

            ws.Cell("A5").Value = "Περιγραφή";
            ws.Cell("B5").Value = panel.Description ?? "";

            ws.Cell("A6").Value = "Κατάσταση";
            ws.Cell("B6").Value = panel.Status ?? "";

            ws.Cell("A7").Value = "Ημερομηνία Export";
            ws.Cell("B7").Value = DateTime.Now;
            ws.Cell("B7").Style.DateFormat.Format = "dd/MM/yyyy HH:mm";

            var infoRange = ws.Range("A3:B7");
            infoRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            infoRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            ws.Range("A3:A7").Style.Font.Bold = true;
            ws.Range("A3:A7").Style.Fill.BackgroundColor = XLColor.AliceBlue;

            int row = 10;

            void WriteSectionHeader(string title)
            {
                ws.Cell(row, 1).Value = title;
                ws.Range(row, 1, row, 10).Merge();
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                row++;

                ws.Cell(row, 1).Value = "Κωδικός";
                ws.Cell(row, 2).Value = "Περιγραφή";
                ws.Cell(row, 3).Value = "Προμηθευτής";
                ws.Cell(row, 4).Value = "Μονάδα";
                ws.Cell(row, 5).Value = "Ποσότητα";
                ws.Cell(row, 6).Value = "Τιμή Μονάδας";
                ws.Cell(row, 7).Value = "ΤΙΜΗ ΚΑΤ";
                ws.Cell(row, 8).Value = "Έκπτωση %";
                ws.Cell(row, 9).Value = "Έκπτωση Αξία";
                ws.Cell(row, 10).Value = "ΤΙΜΗ ΝΕΤ";

                var headerRange = ws.Range(row, 1, row, 10);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                row++;
            }

            void StyleSectionData(int startRow, int endRow)
            {
                if (endRow < startRow)
                    return;

                var dataRange = ws.Range(startRow, 1, endRow, 10);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            }

            WriteSectionHeader("ΗΛΕΚΤΡΟΛΟΓΙΚΑ ΥΛΙΚΑ");

            int materialsStartRow = row;

            foreach (var item in materialRows)
            {
                ws.Cell(row, 1).Value = item.MaterialCode;
                ws.Cell(row, 2).Value = item.Description;
                ws.Cell(row, 3).Value = item.Supplier;
                ws.Cell(row, 4).Value = item.Unit;
                ws.Cell(row, 5).Value = item.Quantity;
                ws.Cell(row, 6).Value = item.UnitPrice;
                ws.Cell(row, 7).Value = item.CatalogTotal;
                ws.Cell(row, 8).Value = item.DiscountPercent / 100m;
                ws.Cell(row, 9).Value = item.DiscountValue;
                ws.Cell(row, 10).Value = item.NetTotal;

                row++;
            }

            int materialsEndRow = row - 1;
            StyleSectionData(materialsStartRow, materialsEndRow);

            row += 2;

            WriteSectionHeader("ΕΡΜΑΡΙΑ");

            int cabinetsStartRow = row;

            foreach (var item in panel.PanelCabinets)
            {
                var catalogTot = item.Quantity * item.UnitPrice;
                var netTot = item.LineNetTotal;
                var discountValue = catalogTot - netTot;

                ws.Cell(row, 1).Value = item.Cabinet?.CabinetCode ?? "";
                ws.Cell(row, 2).Value = item.Cabinet?.Description ?? "";
                ws.Cell(row, 3).Value = item.Supplier?.SupplierName ?? "";
                ws.Cell(row, 4).Value = item.Cabinet?.Unit ?? "pcs";
                ws.Cell(row, 5).Value = item.Quantity;
                ws.Cell(row, 6).Value = item.UnitPrice;
                ws.Cell(row, 7).Value = catalogTot;
                ws.Cell(row, 8).Value = item.DiscountPercent / 100m;
                ws.Cell(row, 9).Value = discountValue;
                ws.Cell(row, 10).Value = netTot;

                row++;
            }

            int cabinetsEndRow = row - 1;
            StyleSectionData(cabinetsStartRow, cabinetsEndRow);

            row += 2;

            WriteSectionHeader("ΛΟΙΠΑ ΥΛΙΚΑ");

            int extraItemsStartRow = row;

            foreach (var item in panel.PanelExtraItems)
            {
                var catalogT = item.Quantity * item.UnitPrice;
                var netT = item.LineNetTotal;
                var discountValue = catalogT - netT;

                ws.Cell(row, 1).Value = item.ItemCode ?? "";
                ws.Cell(row, 2).Value = item.Description;
                ws.Cell(row, 3).Value = "";
                ws.Cell(row, 4).Value = item.Unit;
                ws.Cell(row, 5).Value = item.Quantity;
                ws.Cell(row, 6).Value = item.UnitPrice;
                ws.Cell(row, 7).Value = catalogT;
                ws.Cell(row, 8).Value = item.DiscountPercent / 100m;
                ws.Cell(row, 9).Value = discountValue;
                ws.Cell(row, 10).Value = netT;

                row++;
            }

            int extraItemsEndRow = row - 1;
            StyleSectionData(extraItemsStartRow, extraItemsEndRow);

            var materialsNetTotal = materialRows.Sum(x => x.NetTotal);
            var materialsCatalogTotal = materialRows.Sum(x => x.CatalogTotal);
            var materialsDiscountTotal = materialRows.Sum(x => x.DiscountValue);

            var cabinetsNetTotal = panel.PanelCabinets.Sum(x => x.LineNetTotal);
            var cabinetsCatalogTotal = panel.PanelCabinets.Sum(x => x.Quantity * x.UnitPrice);
            var cabinetsDiscountTotal = cabinetsCatalogTotal - cabinetsNetTotal;

            var extraItemsNetTotal = panel.PanelExtraItems.Sum(x => x.LineNetTotal);
            var extraItemsCatalogTotal = panel.PanelExtraItems.Sum(x => x.Quantity * x.UnitPrice);
            var extraItemsDiscountTotal = extraItemsCatalogTotal - extraItemsNetTotal;

            var catalogTotal = materialsCatalogTotal + cabinetsCatalogTotal + extraItemsCatalogTotal;
            var discountTotal = materialsDiscountTotal + cabinetsDiscountTotal + extraItemsDiscountTotal;
            var netTotal = materialsNetTotal + cabinetsNetTotal + extraItemsNetTotal;

            var finalTotal = netTotal + panel.LaborCost + panel.ProfitAmount;

            row += 2;

            ws.Cell(row, 9).Value = "Υλικά NET";
            ws.Cell(row, 10).Value = materialsNetTotal;
            row++;

            ws.Cell(row, 9).Value = "Ερμάρια NET";
            ws.Cell(row, 10).Value = cabinetsNetTotal;
            row++;

            ws.Cell(row, 9).Value = "Λοιπά Υλικά NET";
            ws.Cell(row, 10).Value = extraItemsNetTotal;
            row++;

            ws.Cell(row, 9).Value = "Εργατικά";
            ws.Cell(row, 10).Value = panel.LaborCost;
            row++;

            ws.Cell(row, 9).Value = "Κέρδος";
            ws.Cell(row, 10).Value = panel.ProfitAmount;
            row++;

            ws.Cell(row, 9).Value = "Σύνολο Κοστολόγησης";
            ws.Cell(row, 10).Value = finalTotal;

            var totalRange = ws.Range(row - 5, 9, row, 10);
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Fill.BackgroundColor = XLColor.LightYellow;
            totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            totalRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Column(5).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(6).Style.NumberFormat.Format = "#,##0.00 €";
            ws.Column(7).Style.NumberFormat.Format = "#,##0.00 €";
            ws.Column(8).Style.NumberFormat.Format = "0.00%";
            ws.Column(9).Style.NumberFormat.Format = "#,##0.00 €";
            ws.Column(10).Style.NumberFormat.Format = "#,##0.00 €";

            ws.Column(1).Width = 20;
            ws.Column(2).Width = 45;
            ws.Column(3).Width = 24;
            ws.Column(4).Width = 12;
            ws.Column(5).Width = 12;
            ws.Column(6).Width = 16;
            ws.Column(7).Width = 16;
            ws.Column(8).Width = 14;
            ws.Column(9).Width = 16;
            ws.Column(10).Width = 18;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        private static string CsvEscape(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        private async Task<List<PanelExportRow>> GetPanelExportRowsAsync(int panelId)
        {
            return await (
                from pm in _context.PanelMaterials
                join m in _context.Materials on pm.MaterialID equals m.MaterialID
                join s in _context.Suppliers on pm.SupplierID equals s.SupplierID into supplierJoin
                from s in supplierJoin.DefaultIfEmpty()
                where pm.PanelID == panelId
                orderby m.MaterialCode
                select new PanelExportRow
                {
                    MaterialCode = m.MaterialCode,
                    Description = m.Description,
                    Supplier = s != null ? s.SupplierName : "",
                    Unit = m.Unit,
                    Quantity = pm.Quantity,
                    UnitPrice = pm.UnitPrice,
                    DiscountPercent = pm.DiscountPercent
                }
            ).ToListAsync();
        }

        private sealed class PanelExportRow
        {
            public string MaterialCode { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Supplier { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal DiscountPercent { get; set; }

            public decimal CatalogTotal => Quantity * UnitPrice;
            public decimal NetTotal => CatalogTotal * (1 - DiscountPercent / 100m);
            public decimal DiscountValue => CatalogTotal - NetTotal;
        }
    }
}