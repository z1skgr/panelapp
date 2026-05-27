using Microsoft.EntityFrameworkCore;
using panelapp.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
namespace panelapp.Services
{
    public class PanelOfferPdfService : IPanelOfferPdfService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;



        public PanelOfferPdfService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }






        public async Task<byte[]> GenerateCustomerOfferPdfAsync(int panelId)
        {
            var panel = await _context.Panels
                .AsNoTracking()
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.PanelID == panelId);

            if (panel == null)
            {
                throw new InvalidOperationException("Panel not found.");
            }

            var materials = await (
                from pm in _context.PanelMaterials.AsNoTracking()
                join m in _context.Materials.AsNoTracking() on pm.MaterialID equals m.MaterialID
                where pm.PanelID == panelId
                orderby m.MaterialCode
                select new
                {
                    m.MaterialCode,
                    m.Description,
                    pm.Quantity
                })
                .ToListAsync();

            var materialsNetTotal = await (
                from pm in _context.PanelMaterials.AsNoTracking()
                where pm.PanelID == panelId
                select pm.Quantity * pm.UnitPrice * (1 - pm.DiscountPercent / 100)
            ).SumAsync();

            var finalOfferTotal = materialsNetTotal + panel.LaborCost + panel.ProfitAmount;

            var logoPath = Path.Combine(
                _env.WebRootPath,
                "images",
                "output-onlinepngtools.png");

            byte[]? logoBytes = null;

            if (File.Exists(logoPath))
            {
                logoBytes = await File.ReadAllBytesAsync(logoPath);
            }

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.ConstantItem(80).Height(45).Element(container =>
                            {
                                if (logoBytes != null)
                                {
                                    container
                                        .AlignLeft()
                                        .AlignMiddle()
                                        .MaxHeight(45)
                                        .MaxWidth(80)
                                        .Image(logoBytes)
                                        .FitArea();
                                }
                            });

                            row.RelativeItem().Column(column =>
                            {
                                column.Item().AlignRight().Text("ΠΡΟΣΦΟΡΑ")
                                    .FontSize(20)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);

                                column.Item().AlignRight().Text($"Ημερομηνία: {DateTime.Now:dd/MM/yyyy}")
                                    .FontSize(9);

                                column.Item().AlignRight().Text("Panel App")
                                    .FontSize(9)
                                    .FontColor(Colors.Grey.Darken1);
                            });
                        });

                        header.Item().PaddingTop(8)
                            .LineHorizontal(1)
                            .LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        column.Spacing(12);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(Card).Column(left =>
                            {
                                left.Item().Text("Στοιχεία Εταιρείας")
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);

                                left.Item().PaddingTop(6).Text("Demo Electrical Company")
                                    .SemiBold();

                                left.Item().Text("Διεύθυνση: Demo Street 123, Αθήνα");
                                left.Item().Text("Τηλέφωνο: 210 0000000");
                                left.Item().Text("Email: info@example.gr");
                                left.Item().Text("ΑΦΜ: 000000000");
                                left.Item().Text("ΔΟΥ: Αθηνών");
                            });

                            row.ConstantItem(15);

                            row.RelativeItem().Element(Card).Column(right =>
                            {
                                right.Item().Text("Στοιχεία Πελάτη / Έργου")
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);

                                right.Item().PaddingTop(6).Text(panel.Customer?.CustomerName ?? panel.CustomerName ?? "-")
                                    .SemiBold();

                                if (!string.IsNullOrWhiteSpace(panel.Customer?.VatNumber))
                                    right.Item().Text($"ΑΦΜ: {panel.Customer.VatNumber}");

                                if (!string.IsNullOrWhiteSpace(panel.Customer?.Address))
                                    right.Item().Text($"Διεύθυνση: {panel.Customer.Address}");

                                if (!string.IsNullOrWhiteSpace(panel.Customer?.Email))
                                    right.Item().Text($"Email: {panel.Customer.Email}");

                                if (!string.IsNullOrWhiteSpace(panel.Customer?.Phone))
                                    right.Item().Text($"Τηλέφωνο: {panel.Customer.Phone}");
                            });
                        });

                        column.Item().Element(Card).Column(project =>
                        {
                            project.Item().Text("Στοιχεία Πίνακα")
                                .FontSize(12)
                                .Bold()
                                .FontColor(Colors.Blue.Darken2);

                            project.Item().PaddingTop(6).Text($"Κωδικός: {panel.PanelCode}")
                                .SemiBold();

                            if (!string.IsNullOrWhiteSpace(panel.Description))
                                project.Item().Text($"Περιγραφή: {panel.Description}");

                            project.Item().Text($"Ημερομηνία προσφοράς: {DateTime.Now:dd/MM/yyyy}");
                        });

                        column.Item().PaddingTop(12).Text("Ανάλυση Υλικών")
                            .FontSize(13)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(90);
                                columns.RelativeColumn();
                                columns.ConstantColumn(70);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Κωδικός");
                                header.Cell().Element(HeaderCell).Text("Περιγραφή");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Ποσότητα");

                            });

                            foreach (var item in materials)
                            {
                                table.Cell().Element(BodyCell).Text(item.MaterialCode);
                                table.Cell().Element(BodyCell).Text(item.Description);
                                table.Cell().Element(BodyCell).AlignRight().Text(item.Quantity.ToString("0.##"));
                            }
                        });

                        column.Item().PaddingTop(18).AlignRight().Width(230).Element(container =>
                        {
                            container
                                .Background(Colors.Green.Lighten5)
                                .Border(1)
                                .BorderColor(Colors.Green.Lighten2)
                                .Padding(12)
                                .Column(total =>
                                {
                                    total.Item().Text("ΣΥΝΟΛΟ ΠΡΟΣΦΟΡΑΣ")
                                        .FontSize(10)
                                        .SemiBold()
                                        .FontColor(Colors.Grey.Darken2);

                                    total.Item().PaddingTop(4).Text($"{finalOfferTotal:N2} €")
                                        .FontSize(20)
                                        .Bold()
                                        .FontColor(Colors.Green.Darken3);
                                });
                        });


                    });

                    page.Footer().Column(footer =>
                    {
                        footer.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        footer.Item().PaddingTop(6).AlignCenter().Text(text =>
                        {
                            text.Span("Η προσφορά δημιουργήθηκε από το Panel App · ")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);

                            text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);
                        });
                    });
                });
            }).GeneratePdf();




        }

        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten3)
                .Padding(5)
                .DefaultTextStyle(x => x.Bold());
        }

        private static IContainer BodyCell(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5);
        }

        private static IContainer Card(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten5)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(12);
        }

        public async Task<byte[]> GenerateInternalCostingPdfAsync(int panelId)
        {
            var panel = await _context.Panels
                .AsNoTracking()
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.PanelID == panelId);

            if (panel == null)
            {
                throw new InvalidOperationException("Panel not found.");
            }

            var materials = await (
                from pm in _context.PanelMaterials.AsNoTracking()
                join m in _context.Materials.AsNoTracking() on pm.MaterialID equals m.MaterialID
                join s in _context.Suppliers.AsNoTracking() on pm.SupplierID equals s.SupplierID into supplierJoin
                from s in supplierJoin.DefaultIfEmpty()
                where pm.PanelID == panelId
                orderby m.MaterialCode
                select new
                {
                    m.MaterialCode,
                    m.Description,
                    SupplierName = s != null ? s.SupplierName : "",
                    pm.Quantity,
                    pm.UnitPrice,
                    pm.DiscountPercent,
                    LineNetTotal = pm.Quantity * pm.UnitPrice * (1 - pm.DiscountPercent / 100),
                    CatalogTotal = pm.Quantity * pm.UnitPrice,

                })
                .ToListAsync();

            var cabinets = await _context.PanelCabinets
                .AsNoTracking()
                .Include(x => x.Cabinet)
                .Include(x => x.Supplier)
                .Where(x => x.PanelID == panelId)
                .OrderBy(x => x.Cabinet!.CabinetCode)
                .ToListAsync();

            var extraItems = await _context.PanelExtraItems
                .AsNoTracking()
                .Where(x => x.PanelID == panelId)
                .OrderBy(x => x.Description)
                .ToListAsync();

            var materialsCatalogTotal = materials.Sum(x => x.CatalogTotal);
            var materialsNetTotal = materials.Sum(x => x.LineNetTotal);

            var cabinetsCatalogTotal = cabinets.Sum(x => x.Quantity * x.UnitPrice);
            var cabinetsNetTotal = cabinets.Sum(x => x.LineNetTotal);

            var extraItemsCatalogTotal = extraItems.Sum(x => x.Quantity * x.UnitPrice);
            var extraItemsNetTotal = extraItems.Sum(x => x.LineNetTotal);

            var catalogTotal =
                materialsCatalogTotal
                + cabinetsCatalogTotal
                + extraItemsCatalogTotal;

            var catalogTotalCat =
                catalogTotal + panel.LaborCost
                + panel.ProfitAmount; ;

            var netTotal =
                materialsNetTotal
                + cabinetsNetTotal
                + extraItemsNetTotal;

            var finalTotal =
                netTotal
                + panel.LaborCost
                + panel.ProfitAmount;



            var logoPath = Path.Combine(
                _env.WebRootPath,
                "images",
                "output-onlinepngtools.png");

            byte[]? logoBytes = null;

            if (File.Exists(logoPath))
            {
                logoBytes = await File.ReadAllBytesAsync(logoPath);
            }

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.ConstantItem(80).Height(45).Element(container =>
                            {
                                if (logoBytes != null)
                                {
                                    container
                                        .AlignLeft()
                                        .AlignMiddle()
                                        .MaxHeight(45)
                                        .MaxWidth(80)
                                        .Image(logoBytes)
                                        .FitArea();
                                }
                            });

                            row.RelativeItem().Column(column =>
                            {
                                column.Item().AlignRight().Text("ΕΣΩΤΕΡΙΚΗ ΚΟΣΤΟΛΟΓΗΣΗ")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);

                                column.Item().AlignRight().Text($"Πίνακας: {panel.PanelCode}")
                                    .FontSize(10);

                                column.Item().AlignRight().Text($"Ημερομηνία: {DateTime.Now:dd/MM/yyyy}")
                                    .FontSize(9);
                            });
                        });

                        header.Item().PaddingTop(8)
                            .LineHorizontal(1)
                            .LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        column.Spacing(12);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(Card).Column(left =>
                            {
                                left.Item().Text("Στοιχεία Πίνακα")
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);

                                left.Item().PaddingTop(6).Text($"Κωδικός: {panel.PanelCode}")
                                    .SemiBold();

                                left.Item().Text($"Κατάσταση: {panel.Status}");

                                if (!string.IsNullOrWhiteSpace(panel.Description))
                                    left.Item().Text($"Περιγραφή: {panel.Description}");
                            });

                            row.ConstantItem(15);

                            row.RelativeItem().Element(Card).Column(right =>
                            {
                                right.Item().Text("Πελάτης")
                                    .FontSize(12)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken2);

                                right.Item().PaddingTop(6).Text(panel.Customer?.CustomerName ?? panel.CustomerName ?? "-")
                                    .SemiBold();

                                if (!string.IsNullOrWhiteSpace(panel.Customer?.VatNumber))
                                    right.Item().Text($"ΑΦΜ: {panel.Customer.VatNumber}");

                                if (!string.IsNullOrWhiteSpace(panel.Customer?.Phone))
                                    right.Item().Text($"Τηλέφωνο: {panel.Customer.Phone}");
                            });
                        });

                        column.Item().PaddingTop(12).Text("Ανάλυση Υλικών")
                            .FontSize(13)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        AddInternalSection(
                            column,
                            "ΗΛΕΚΤΡΟΛΟΓΙΚΑ ΥΛΙΚΑ",
                            materials,
                            x => x.MaterialCode,
                            x => x.Description,
                            x => x.SupplierName,
                            x => x.Quantity,
                            x => x.UnitPrice,
                            x => x.DiscountPercent,
                            x => x.CatalogTotal,
                            x => x.LineNetTotal);

                        AddInternalSection(
                            column,
                            "ΕΡΜΑΡΙΑ",
                            cabinets,
                            x => x.Cabinet?.CabinetCode ?? "",
                            x => x.Cabinet?.Description ?? "",
                            x => x.Supplier?.SupplierName ?? "",
                            x => x.Quantity,
                            x => x.UnitPrice,
                            x => x.DiscountPercent,
                            x => x.Quantity * x.UnitPrice,
                            x => x.LineNetTotal);

                        AddInternalSection(
                            column,
                            "ΛΟΙΠΑ ΥΛΙΚΑ",
                            extraItems,
                            x => x.ItemCode ?? "",
                            x => x.Description,
                            x => "",
                            x => x.Quantity,
                            x => x.UnitPrice,
                            x => x.DiscountPercent,
                            x => x.Quantity * x.UnitPrice,
                            x => x.LineNetTotal);

                        column.Item().PaddingTop(18).Row(summary =>
                        {
                            summary.RelativeItem().Background(Colors.Blue.Lighten5).Border(1).BorderColor(Colors.Blue.Lighten2).Padding(10).Column(col =>
                            {
                                col.Item().Text("ΣΥΝΟΛΑ ΚΑΤΑΛΟΓΟΥ").Bold();

                                col.Item().PaddingTop(5).Row(row =>
                                {
                                    row.RelativeItem().Text("Υλικά");
                                    row.ConstantItem(90).AlignRight().Text($"{materialsCatalogTotal:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Ερμάρια");
                                    row.ConstantItem(90).AlignRight().Text($"{cabinetsCatalogTotal:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Λοιπά Υλικά");
                                    row.ConstantItem(90).AlignRight().Text($"{extraItemsCatalogTotal:N2} €");
                                });
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Εργατικά");
                                    row.ConstantItem(90).AlignRight().Text($"{panel.LaborCost:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Κέρδος");
                                    row.ConstantItem(90).AlignRight().Text($"{panel.ProfitAmount:N2} €");
                                });



                                col.Item().PaddingVertical(5).LineHorizontal(1);

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Σύνολο Καταλόγου").Bold();
                                    row.ConstantItem(90).AlignRight().Text($"{catalogTotalCat:N2} €").Bold();
                                });
                            });

                            summary.ConstantItem(15);

                            summary.RelativeItem().Background(Colors.Yellow.Lighten5).Border(1).BorderColor(Colors.Yellow.Darken1).Padding(10).Column(col =>
                            {
                                col.Item().Text("NET ΚΟΣΤΟΛΟΓΗΣΗ").Bold();

                                col.Item().PaddingTop(5).Row(row =>
                                {
                                    row.RelativeItem().Text("Υλικά NET");
                                    row.ConstantItem(90).AlignRight().Text($"{materialsNetTotal:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Ερμάρια NET");
                                    row.ConstantItem(90).AlignRight().Text($"{cabinetsNetTotal:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Λοιπά Υλικά NET");
                                    row.ConstantItem(90).AlignRight().Text($"{extraItemsNetTotal:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Εργατικά");
                                    row.ConstantItem(90).AlignRight().Text($"{panel.LaborCost:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Κέρδος");
                                    row.ConstantItem(90).AlignRight().Text($"{panel.ProfitAmount:N2} €");
                                });

                                col.Item().PaddingVertical(5).LineHorizontal(1);

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Σύνολο NET").Bold();
                                    row.ConstantItem(90).AlignRight().Text($"{finalTotal:N2} €").Bold();
                                });
                            });
                        });
                    });

                    page.Footer().Column(footer =>
                    {
                        footer.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        footer.Item().PaddingTop(6).AlignCenter().Text(text =>
                        {
                            text.Span("Εσωτερική κοστολόγηση · ")
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);

                            text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                                .FontSize(8)
                                .FontColor(Colors.Grey.Darken1);
                        });
                    });
                });
            }).GeneratePdf();



        }

        private static void AddInternalSection<T>(
    ColumnDescriptor column,
    string title,
    IEnumerable<T> items,
    Func<T, string> code,
    Func<T, string> description,
    Func<T, string> supplier,
    Func<T, decimal> quantity,
    Func<T, decimal> unitPrice,
    Func<T, decimal> discountPercent,
    Func<T, decimal> catalogTotal,
    Func<T, decimal> netTotal)
        {
            var list = items.ToList();

            if (!list.Any())
                return;

            column.Item().PaddingTop(10).Text(title)
                .FontSize(12)
                .Bold()
                .FontColor(Colors.Blue.Darken2);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(60);
                    columns.RelativeColumn(2.2f);
                    columns.RelativeColumn(1.3f);
                    columns.ConstantColumn(45);
                    columns.ConstantColumn(55);
                    columns.ConstantColumn(45);
                    columns.ConstantColumn(65);
                    columns.ConstantColumn(65);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Κωδ.");
                    header.Cell().Element(HeaderCell).Text("Περιγραφή");
                    header.Cell().Element(HeaderCell).Text("Προμηθ.");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Ποσ.");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Τιμή");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Εκπτ.");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Κατ.");
                    header.Cell().Element(HeaderCell).AlignRight().Text("NET");
                });

                foreach (var item in list)
                {
                    table.Cell().Element(BodyCell).Text(code(item));
                    table.Cell().Element(BodyCell).Text(description(item));
                    table.Cell().Element(BodyCell).Text(supplier(item));
                    table.Cell().Element(BodyCell).AlignRight().Text(quantity(item).ToString("N2"));
                    table.Cell().Element(BodyCell).AlignRight().Text($"{unitPrice(item):N2}");
                    table.Cell().Element(BodyCell).AlignRight().Text($"{discountPercent(item):N2}%");
                    table.Cell().Element(BodyCell).AlignRight().Text($"{catalogTotal(item):N2}");
                    table.Cell().Element(BodyCell).AlignRight().Text($"{netTotal(item):N2}");
                }
            });
        }
    }
}