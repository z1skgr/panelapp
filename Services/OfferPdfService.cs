using Microsoft.EntityFrameworkCore;
using panelapp.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace panelapp.Services
{
    public class OfferPdfService : IOfferPdfService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public OfferPdfService(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        public async Task<byte[]> GenerateCustomerOfferPdfAsync(int offerId)
        {
            var offer = await _context.Offers
                .Include(x => x.Customer)
                .Include(x => x.OfferMaterials)
                    .ThenInclude(x => x.Material)
                .Include(x => x.OfferCabinets)
                    .ThenInclude(x => x.Cabinet)
                .Include(x => x.OfferExtraItems)
                .FirstOrDefaultAsync(x => x.OfferID == offerId);

            if (offer == null)
                return Array.Empty<byte>();

            var finalTotal =
                offer.OfferMaterials.Sum(x => x.OriginalTotalPrice)
                + offer.OfferCabinets.Sum(x => x.OriginalTotalPrice)
                + offer.OfferExtraItems.Sum(x => x.OriginalTotalPrice)
                + offer.LaborCost
                + offer.ProfitAmount;

            var logoPath = Path.Combine(
                _environment.WebRootPath,
                "images",
                "output-onlinepngtools.png"
            );

            var hasLogo = File.Exists(logoPath);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("ΠΡΟΣΦΟΡΑ")
                                .FontSize(20)
                                .Bold();

                            col.Item().Text(offer.OfferCode)
                                .FontSize(12)
                                .FontColor(Colors.Grey.Darken2);
                        });

                        row.ConstantItem(120).AlignRight().Column(col =>
                        {
                            if (hasLogo)
                            {
                                col.Item()
                                    .Height(55)
                                    .Image(logoPath)
                                    .FitArea();
                            }

                            col.Item().AlignRight()
                                .Text(DateTime.Now.ToString("dd/MM/yyyy"));
                        });
                    });

                    page.Content().PaddingVertical(20).Column(content =>
                    {
                        content.Spacing(15);

                        content.Item().Text($"Πελάτης: {offer.CustomerName}");
                        content.Item().Text($"Περιγραφή: {offer.Description}");

                        AddCustomerSection(
                            content,
                            "Ηλεκτρολογικά Υλικά",
                            offer.OfferMaterials.Select(x => new
                            {
                                Code = x.Material?.MaterialCode ?? "",
                                Description = x.Material?.Description ?? "",
                                Quantity = x.Quantity
                            }));

                        AddCustomerSection(
                            content,
                            "Ερμάρια",
                            offer.OfferCabinets.Select(x => new
                            {
                                Code = x.Cabinet?.CabinetCode ?? "",
                                Description = x.Cabinet?.Description ?? "",
                                Quantity = x.Quantity
                            }));

                        AddCustomerSection(
                            content,
                            "Λοιπά Υλικά",
                            offer.OfferExtraItems.Select(x => new
                            {
                                Code = x.ItemCode ?? "",
                                Description = x.Description,
                                Quantity = x.Quantity
                            }));

                        content.Item()
                            .AlignRight()
                            .Width(220)
                            .Background(Colors.Grey.Lighten3)
                            .Padding(10)
                            .Column(col =>
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem()
                                        .Text("Συνολικό Κόστος")
                                        .Bold();

                                    row.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{finalTotal:N2} €")
                                        .Bold();
                                });
                            });
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Σελίδα ");
                            x.CurrentPageNumber();
                            x.Span(" από ");
                            x.TotalPages();
                        });
                });
            }).GeneratePdf();
        }

        public async Task<byte[]> GenerateInternalCostingPdfAsync(int offerId)
        {
            var offer = await _context.Offers
                .Include(x => x.Customer)
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

            var materialsNetTotal = offer.OfferMaterials.Sum(x => x.LineNetTotal);
            var cabinetsNetTotal = offer.OfferCabinets.Sum(x => x.LineNetTotal);
            var extraItemsNetTotal = offer.OfferExtraItems.Sum(x => x.LineNetTotal);

            var finalTotal =
                materialsNetTotal
                + cabinetsNetTotal
                + extraItemsNetTotal
                + offer.LaborCost
                + offer.ProfitAmount;

            var logoPath = Path.Combine(
                _environment.WebRootPath,
                "images",
                "output-onlinepngtools.png"
            );

            var hasLogo = File.Exists(logoPath);

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
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("ΠΡΟΣΦΟΡΑ")
                                    .FontSize(20)
                                    .Bold();

                                col.Item().Text(offer.OfferCode)
                                    .FontSize(12)
                                    .FontColor(Colors.Grey.Darken2);
                            });

                            row.ConstantItem(120).AlignRight().Column(col =>
                            {
                                if (hasLogo)
                                {
                                    col.Item()
                                        .Height(55)
                                        .Image(logoPath)
                                        .FitArea();
                                }

                                col.Item().AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy"));
                            });
                        });

                        header.Item().PaddingTop(10).LineHorizontal(1);
                    });

                    page.Content().PaddingVertical(20).Column(content =>
                    {
                        content.Spacing(15);

                        content.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Στοιχεία Πελάτη").Bold();
                                col.Item().Text(offer.CustomerName);

                                if (offer.Customer != null)
                                {
                                    if (!string.IsNullOrWhiteSpace(offer.Customer.VatNumber))
                                        col.Item().Text($"ΑΦΜ: {offer.Customer.VatNumber}");

                                    if (!string.IsNullOrWhiteSpace(offer.Customer.Phone))
                                        col.Item().Text($"Τηλ.: {offer.Customer.Phone}");

                                    if (!string.IsNullOrWhiteSpace(offer.Customer.Email))
                                        col.Item().Text($"Email: {offer.Customer.Email}");

                                    if (!string.IsNullOrWhiteSpace(offer.Customer.Address))
                                        col.Item().Text($"Διεύθυνση: {offer.Customer.Address}");
                                }
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Στοιχεία Προσφοράς").Bold();
                                col.Item().Text($"Κωδικός: {offer.OfferCode}");
                                col.Item().Text($"Ημερομηνία: {offer.CreatedDate:dd/MM/yyyy}");
                                col.Item().Text($"Κατάσταση: {offer.Status}");
                            });
                        });

                        if (!string.IsNullOrWhiteSpace(offer.Description))
                        {
                            content.Item().Column(col =>
                            {
                                col.Item().Text("Περιγραφή").Bold();
                                col.Item().Text(offer.Description);
                            });
                        }

                        content.Item().Text("Ηλεκτρολογικά Υλικά")
                        .FontSize(12)
                        .Bold();

                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.1f); // Κωδικός
                                columns.RelativeColumn(3.2f); // Περιγραφή
                                columns.RelativeColumn(.7f);  // Ποσ.
                                columns.RelativeColumn(1f);   // Τιμή
                                columns.RelativeColumn(.8f);  // Εκπτ.
                                columns.RelativeColumn(1.1f); // NET
                                columns.RelativeColumn(1.1f); // ΚΑΤ
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Κωδικός");
                                header.Cell().Element(HeaderCell).Text("Περιγραφή");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Ποσ.");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Τιμή");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Εκπτ.");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Σύνολο NET");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Σύνολο ΚΑΤ");
                            });

                            foreach (var item in offer.OfferMaterials)
                            {
                                table.Cell().Element(BodyCell).Text(item.Material?.MaterialCode ?? "");
                                table.Cell().Element(BodyCell).Text(item.Material?.Description ?? "");
                                table.Cell().Element(BodyCell).AlignRight().Text(item.Quantity.ToString("N2"));
                                table.Cell().Element(BodyCell).AlignRight().Text($"{item.UnitPrice:N2} €");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{item.DiscountPercent:N2}%");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{item.LineNetTotal:N2} €");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{item.OriginalTotalPrice:N2} €");

                            }
                        });


                        content.Item().Text("Ερμάρια")
                            .FontSize(12)
                            .Bold();

                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.1f); // Κωδικός
                                columns.RelativeColumn(3.2f); // Περιγραφή
                                columns.RelativeColumn(.7f);  // Ποσ.
                                columns.RelativeColumn(1f);   // Τιμή
                                columns.RelativeColumn(.8f);  // Εκπτ.
                                columns.RelativeColumn(1.1f); // NET
                                columns.RelativeColumn(1.1f); // ΚΑΤ
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Κωδικός");
                                header.Cell().Element(HeaderCell).Text("Περιγραφή");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Ποσ.");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Τιμή");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Εκπτ.");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Σύνολο NET");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Σύνολο ΚΑΤ");
                            });

                            foreach (var item in offer.OfferCabinets)
                            {
                                table.Cell().Element(BodyCell).Text(item.Cabinet?.CabinetCode ?? "");
                                table.Cell().Element(BodyCell).Text(item.Cabinet?.Description ?? "");
                                table.Cell().Element(BodyCell).AlignRight().Text(item.Quantity.ToString("N2"));
                                table.Cell().Element(BodyCell).AlignRight().Text($"{item.UnitPrice:N2} €");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{item.DiscountPercent:N2}%");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{item.LineNetTotal:N2} €");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{item.OriginalTotalPrice:N2} €");
                            }
                        });




                        content.Item().Text("Λοιπά Υλικά")
                            .FontSize(12)
                            .Bold();

                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.1f); // Κωδικός
                                columns.RelativeColumn(3.2f); // Περιγραφή
                                columns.RelativeColumn(.7f);  // Ποσ.
                                columns.RelativeColumn(1f);   // Τιμή
                                columns.RelativeColumn(.8f);  // Εκπτ.
                                columns.RelativeColumn(1.1f); // NET
                                columns.RelativeColumn(1.1f); // ΚΑΤ
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Κωδικός");
                                header.Cell().Element(HeaderCell).Text("Περιγραφή");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Ποσ.");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Τιμή");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Εκπτ.");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Σύνολο NET");
                                header.Cell().Element(HeaderCell).AlignRight().Text("Σύνολο ΚΑΤ");
                            });

                            foreach (var item in offer.OfferExtraItems)
                            {
                                table.Cell().Element(BodyCell).Text(item.ItemCode ?? "");
                                table.Cell().Element(BodyCell).Text(item.Description);
                                table.Cell().Element(BodyCell).AlignRight().Text(item.Quantity.ToString("N2"));
                                table.Cell().Element(BodyCell).AlignRight().Text($"{item.UnitPrice:N2} €");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{item.DiscountPercent:N2}%");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{item.LineNetTotal:N2} €");
                                table.Cell().Element(BodyCell).AlignRight().Text($"{item.OriginalTotalPrice:N2} €");
                            }
                        });







                        content.Item().Row(summary =>
                        {
                            summary.RelativeItem().Background(Colors.Blue.Lighten4).Padding(10).Column(col =>
                            {
                                var materialsCatalogTotal =
                                    offer.OfferMaterials.Sum(x => x.OriginalTotalPrice);

                                var cabinetsCatalogTotal =
                                    offer.OfferCabinets.Sum(x => x.OriginalTotalPrice);

                                var extraItemsCatalogTotal =
                                    offer.OfferExtraItems.Sum(x => x.OriginalTotalPrice);

                                var grandCatalogTotal =
                                    materialsCatalogTotal
                                    + cabinetsCatalogTotal
                                    + extraItemsCatalogTotal;

                                col.Item().Text("ΣΥΝΟΛΑ ΚΑΤΑΛΟΓΟΥ")
                                    .Bold()
                                    .FontSize(11);

                                col.Item().PaddingTop(5).Row(row =>
                                {
                                    row.RelativeItem().Text("Υλικά Καταλόγου");
                                    row.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{materialsCatalogTotal:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Ερμάρια Καταλόγου");
                                    row.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{cabinetsCatalogTotal:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Λοιπά Υλικά Καταλόγου");
                                    row.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{extraItemsCatalogTotal:N2} €");
                                });
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Εργατικά");
                                    row.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{offer.LaborCost:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Κέρδος");
                                    row.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{offer.ProfitAmount:N2} €");
                                });

                                col.Item().PaddingTop(5).LineHorizontal(1);

                                col.Item().PaddingTop(5).Row(row =>
                                {
                                    row.RelativeItem().Text("Σύνολο Καταλόγου").Bold();

                                    row.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{grandCatalogTotal:N2} €")
                                        .Bold();
                                });
                            });

                            summary.ConstantItem(20);

                            summary.RelativeItem().Background(Colors.Yellow.Lighten4).Padding(10).Column(col =>
                            {
                                col.Item().Text("NET ΚΟΣΤΟΛΟΓΗΣΗ")
                                    .Bold()
                                    .FontSize(11);

                                col.Item().PaddingTop(5).Row(row =>
                                {
                                    row.RelativeItem().Text("Υλικά NET");
                                    row.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{materialsNetTotal:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Ερμάρια NET");
                                    row.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{cabinetsNetTotal:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Λοιπά Υλικά NET");
                                    row.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{extraItemsNetTotal:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Εργατικά");
                                    row.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{offer.LaborCost:N2} €");
                                });

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Κέρδος");
                                    row.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{offer.ProfitAmount:N2} €");
                                });

                                col.Item().PaddingTop(5).LineHorizontal(1);

                                col.Item().PaddingTop(5).Row(row =>
                                {
                                    row.RelativeItem().Text("Σύνολο NET").Bold();

                                    row.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{finalTotal:N2} €")
                                        .Bold();
                                });
                            });
                        });

                        if (!string.IsNullOrWhiteSpace(offer.Notes))
                        {
                            content.Item().Column(col =>
                            {
                                col.Item().Text("Σημειώσεις").Bold();
                                col.Item().Text(offer.Notes);
                            });
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Σελίδα ");
                            x.CurrentPageNumber();
                            x.Span(" από ");
                            x.TotalPages();
                        });
                });
            }).GeneratePdf();



            static IContainer HeaderCell(IContainer container)
            {
                return container
                    .DefaultTextStyle(x => x.Bold())
                    .Background(Colors.Grey.Lighten3)
                    .Padding(5)
                    .BorderBottom(1);
            }

            static IContainer BodyCell(IContainer container)
            {
                return container
                    .PaddingVertical(4)
                    .PaddingHorizontal(5)
                    .BorderBottom(0.5f)
                    .BorderColor(Colors.Grey.Lighten2);
            }




        }

        private static void AddCustomerSection<T>(
    ColumnDescriptor content,
    string title,
    IEnumerable<T> items)
        {
            var list = items.Cast<dynamic>().ToList();

            if (!list.Any())
                return;

            content.Item()
                .Background(Colors.Grey.Lighten3)
                .Padding(6)
                .Text(title)
                .FontSize(12)
                .Bold();

            content.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(5f);
                    columns.RelativeColumn(1f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(CustomerHeaderCell).Text("Κωδικός");
                    header.Cell().Element(CustomerHeaderCell).Text("Περιγραφή");
                    header.Cell().Element(CustomerHeaderCell).AlignRight().Text("Ποσ.");
                });

                foreach (var item in list)
                {
                    table.Cell().Element(CustomerBodyCell).Text((string)item.Code);
                    table.Cell().Element(CustomerBodyCell).Text((string)item.Description);
                    table.Cell().Element(CustomerBodyCell).AlignRight().Text(((decimal)item.Quantity).ToString("N2"));
                }
            });
        }

        private static IContainer CustomerHeaderCell(IContainer container)
        {
            return container
                .DefaultTextStyle(x => x.Bold())
                .Background(Colors.Grey.Lighten3)
                .Padding(5)
                .BorderBottom(1);
        }

        private static IContainer CustomerBodyCell(IContainer container)
        {
            return container
                .PaddingVertical(4)
                .PaddingHorizontal(5)
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2);
        }
    }
}