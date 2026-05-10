namespace panelapp.Services;

using Microsoft.AspNetCore.Http;

public interface IMaterialImportService
{
    Task<MaterialImportResult> ImportAsync(int supplierId, IFormFile excelFile);
}
