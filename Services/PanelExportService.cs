using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using panelapp.Data;
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
            var rows = await GetPanelExportRowsAsync(panelId);

            var csv = new StringBuilder();

            csv.AppendLine("Κωδικός,Περιγραφή,Προμηθευτής,Ποσότητα,Τιμή Μονάδας,ΤΙΜΗ ΚΑΤ,Έκπτωση %,Έκπτωση Αξία,ΤΙΜΗ ΝΕΤ");

            decimal totalCatalog = 0m;
            decimal totalDiscount = 0m;
            decimal totalNet = 0m;

            foreach (var item in rows)
            {
                totalCatalog += item.CatalogTotal;
                totalDiscount += item.DiscountValue;
                totalNet += item.NetTotal;

                csv.AppendLine(string.Join(",",
                    CsvEscape(item.MaterialCode),
                    CsvEscape(item.Description),
                    CsvEscape(item.Supplier),
                    item.Quantity.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    item.UnitPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    item.CatalogTotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    item.DiscountPercent.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    item.DiscountValue.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    item.NetTotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                ));
            }

            csv.AppendLine();
            csv.AppendLine($"ΣΥΝΟΛΟ ΤΙΜΗ ΚΑΤ,,,,,{totalCatalog.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");
            csv.AppendLine($"ΣΥΝΟΛΟ ΕΚΠΤΩΣΗΣ,,,,,,,{totalDiscount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");
            csv.AppendLine($"ΣΥΝΟΛΟ ΤΙΜΗ ΝΕΤ,,,,,,,,{totalNet.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");

            var preamble = Encoding.UTF8.GetPreamble();
            var content = Encoding.UTF8.GetBytes(csv.ToString());

            return preamble.Concat(content).ToArray();
        }

        public async Task<byte[]> ExportExcelAsync(int panelId)
        {
            var panel = await _context.Panels
                .FirstOrDefaultAsync(p => p.PanelID == panelId);

            if (panel == null)
            {
                return Array.Empty<byte>();
            }

            var rows = await GetPanelExportRowsAsync(panelId);

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

            int headerRow = 10;

            ws.Cell(headerRow, 1).Value = "Κωδικός";
            ws.Cell(headerRow, 2).Value = "Περιγραφή";
            ws.Cell(headerRow, 3).Value = "Προμηθευτής";
            ws.Cell(headerRow, 4).Value = "Μονάδα";
            ws.Cell(headerRow, 5).Value = "Ποσότητα";
            ws.Cell(headerRow, 6).Value = "Τιμή Μονάδας";
            ws.Cell(headerRow, 7).Value = "ΤΙΜΗ ΚΑΤ";
            ws.Cell(headerRow, 8).Value = "Έκπτωση %";
            ws.Cell(headerRow, 9).Value = "Έκπτωση Αξία";
            ws.Cell(headerRow, 10).Value = "ΤΙΜΗ ΝΕΤ";

            var headerRange = ws.Range(headerRow, 1, headerRow, 10);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            int row = headerRow + 1;

            foreach (var item in rows)
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

            int dataStartRow = headerRow + 1;
            int dataEndRow = row - 1;

            if (dataEndRow >= dataStartRow)
            {
                var dataRange = ws.Range(dataStartRow, 1, dataEndRow, 10);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            }

            ws.Cell(row, 6).Value = "Σύνολα";
            ws.Cell(row, 6).Style.Font.Bold = true;
            ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            ws.Cell(row, 7).FormulaA1 = dataEndRow >= dataStartRow ? $"SUM(G{dataStartRow}:G{dataEndRow})" : "0";
            ws.Cell(row, 9).FormulaA1 = dataEndRow >= dataStartRow ? $"SUM(I{dataStartRow}:I{dataEndRow})" : "0";
            ws.Cell(row, 10).FormulaA1 = dataEndRow >= dataStartRow ? $"SUM(J{dataStartRow}:J{dataEndRow})" : "0";

            var totalRange = ws.Range(row, 6, row, 10);
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