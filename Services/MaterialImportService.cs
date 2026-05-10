using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using panelapp.Constants;
using panelapp.Data;
using panelapp.Models;
using System.Globalization;

namespace panelapp.Services
{
    public class MaterialImportService : IMaterialImportService
    {
        private readonly ApplicationDbContext _context;

        public MaterialImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<MaterialImportResult> ImportAsync(int supplierId, IFormFile excelFile)
        {
            var result = new MaterialImportResult();

            var inserted = 0;
            var updated = 0;
            var skipped = 0;

            var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using var stream = new MemoryStream();
            await excelFile.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);

            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

            if (lastRow < 2)
            {
                result.Success = false;
                result.Messages.Add("Το αρχείο δεν περιέχει δεδομένα.");
                return result;
            }

            var headerRow = worksheet.Row(1);
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var cell in headerRow.CellsUsed())
            {
                var header = cell.GetString().Trim();

                if (!string.IsNullOrWhiteSpace(header))
                {
                    headerMap[header] = cell.Address.ColumnNumber;
                }
            }

            string[] requiredHeaders = { "MaterialCode", "Description", "Price", "Unit" };

            foreach (var required in requiredHeaders)
            {
                if (!headerMap.ContainsKey(required))
                {
                    result.Success = false;
                    result.Messages.Add($"Λείπει η στήλη: {required}");
                    return result;
                }
            }

            var existingMaterials = await _context.Materials
                .Where(m => m.SupplierID == supplierId)
                .ToDictionaryAsync(m => m.MaterialCode, StringComparer.OrdinalIgnoreCase);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.Now;

                for (int rowNumber = 2; rowNumber <= lastRow; rowNumber++)
                {
                    var row = worksheet.Row(rowNumber);

                    var materialCode = row.Cell(headerMap["MaterialCode"]).GetString().Trim().ToUpperInvariant();
                    var description = row.Cell(headerMap["Description"]).GetString().Trim();
                    var priceText = row.Cell(headerMap["Price"]).GetString().Trim();
                    var unitRaw = row.Cell(headerMap["Unit"]).GetString().Trim();
                    var unit = MaterialUnits.NormalizeUnit(unitRaw);

                    if (string.IsNullOrWhiteSpace(materialCode) ||
                        string.IsNullOrWhiteSpace(description) ||
                        string.IsNullOrWhiteSpace(priceText) ||
                        string.IsNullOrWhiteSpace(unitRaw))
                    {
                        skipped++;
                        result.Messages.Add($"Γραμμή {rowNumber}: λείπουν υποχρεωτικά πεδία.");
                        continue;
                    }

                    if (unit == null)
                    {
                        skipped++;
                        result.Messages.Add($"Γραμμή {rowNumber}: μη έγκυρη μονάδα '{unitRaw}'. Επιτρέπονται μόνο pcs ή meters.");
                        continue;
                    }

                    if (!seenCodes.Add(materialCode))
                    {
                        skipped++;
                        result.Messages.Add($"Γραμμή {rowNumber}: διπλό MaterialCode '{materialCode}' μέσα στο ίδιο αρχείο.");
                        continue;
                    }

                    if (!decimal.TryParse(priceText.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    {
                        skipped++;
                        result.Messages.Add($"Γραμμή {rowNumber}: μη έγκυρη τιμή Price.");
                        continue;
                    }

                    if (price < 0)
                    {
                        skipped++;
                        result.Messages.Add($"Γραμμή {rowNumber}: αρνητική τιμή Price.");
                        continue;
                    }

                    if (!existingMaterials.TryGetValue(materialCode, out var existing))
                    {
                        var material = new Material
                        {
                            MaterialCode = materialCode,
                            Description = description,
                            CurrentPrice = price,
                            Unit = unit,
                            SupplierID = supplierId,
                            Active = true,
                            CreatedDate = now,
                            LastModifiedDate = now,
                            PriceUpdatedDate = now
                        };

                        _context.Materials.Add(material);
                        existingMaterials[materialCode] = material;

                        inserted++;
                    }
                    else
                    {
                        var hasChanges =
                            existing.Description != description ||
                            existing.Unit != unit ||
                            existing.CurrentPrice != price ||
                            existing.Active != true;

                        if (!hasChanges)
                        {
                            skipped++;
                            continue;
                        }

                        if (existing.CurrentPrice != price)
                        {
                            existing.PriceUpdatedDate = now;
                        }

                        existing.Description = description;
                        existing.Unit = unit;
                        existing.CurrentPrice = price;
                        existing.Active = true;
                        existing.LastModifiedDate = now;

                        updated++;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            result.InsertedCount = inserted;
            result.UpdatedCount = updated;
            result.SkippedCount = skipped;
            result.Messages.Insert(0, $"Η εισαγωγή ολοκληρώθηκε. Νέα: {inserted}, Ενημερώσεις: {updated}, Παραλείψεις: {skipped}");

            return result;
        }
    }
}